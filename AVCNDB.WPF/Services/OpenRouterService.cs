using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AVCNDB.WPF.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AVCNDB.WPF.Services;

public class OpenRouterService : IOpenRouterService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    private const string CompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("OpenRouter:ApiKey est manquant dans secrets.json");
        _model = configuration["OpenRouter:Model"] ?? "nvidia/nemotron-3-super-120b-a12b";
    }

    public string ModelName => _model;

    public async Task<InteractionAnalysis> AnalyzeInteractionAsync(
        string dci1, string voie1,
        string dci2, string voie2,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dci1)) throw new ArgumentException("DCI 1 ne peut pas être vide.", nameof(dci1));
        if (string.IsNullOrWhiteSpace(dci2)) throw new ArgumentException("DCI 2 ne peut pas être vide.", nameof(dci2));

        var drug1 = string.IsNullOrWhiteSpace(voie1) ? dci1 : $"{dci1} (voie : {voie1})";
        var drug2 = string.IsNullOrWhiteSpace(voie2) ? dci2 : $"{dci2} (voie : {voie2})";

        // Nemotron 3 Super has a built-in reasoning chain that consumes ~400-600 tokens
        // before generating its answer. max_tokens must cover both reasoning AND the JSON
        // response. 2500 gives comfortable headroom without wasting budget.
        var prompt =
            $"Analyse l'interaction médicamenteuse entre {drug1} et {drug2}.\n" +
            "Réponds UNIQUEMENT avec un objet JSON valide (sans bloc Markdown ni texte autour), " +
            "avec exactement ces quatre champs (valeurs en français, concises) :\n" +
            "{\n" +
            "  \"level\": \"Contre-indiqué|Déconseillé|Précaution|A surveiller\",\n" +
            "  \"description\": \"<max 120 mots>\",\n" +
            "  \"mecanisme\": \"<max 80 mots>\",\n" +
            "  \"conduite\": \"<max 100 mots>\"\n" +
            "}";

        var requestBody = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.2,
            max_tokens = 2500
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("HTTP-Referer", "https://avcndb.local");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        // Guard: API must return at least one choice
        if (!root.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
            throw new InvalidOperationException("OpenRouter a renvoyé une réponse sans choix. Réessayez.");

        var choice = choicesEl[0];

        // Detect truncated response (model hit token limit before finishing)
        var finishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;
        if (finishReason == "length")
            throw new InvalidOperationException(
                "La réponse du modèle a été tronquée (limite de tokens atteinte). " +
                "Réessayez — les réponses varient en longueur.");

        if (!choice.TryGetProperty("message", out var msgEl) ||
            !msgEl.TryGetProperty("content", out var contentEl))
            throw new InvalidOperationException("Structure de réponse OpenRouter inattendue.");

        var content = contentEl.GetString() ?? string.Empty;

        return ParseAnalysis(content, _model);
    }

    private static InteractionAnalysis ParseAnalysis(string raw, string modelName)
    {
        try
        {
            var clean = raw.Trim();
            // Strip markdown code fences if present
            if (clean.StartsWith("```"))
            {
                var firstNewline = clean.IndexOf('\n');
                if (firstNewline >= 0) clean = clean[(firstNewline + 1)..];
            }
            if (clean.EndsWith("```"))
                clean = clean[..clean.LastIndexOf("```")].TrimEnd();

            using var doc = JsonDocument.Parse(clean.Trim());
            var root = doc.RootElement;

            var rawLevel = root.TryGetProperty("level", out var l) ? l.GetString() ?? "" : "";
            return new InteractionAnalysis(
                Level:       NormalizeLevel(rawLevel),
                Description: root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                Mecanisme:   root.TryGetProperty("mecanisme",   out var m) ? m.GetString() ?? "" : "",
                Conduite:    root.TryGetProperty("conduite",    out var c) ? c.GetString() ?? "" : "",
                RawText:     raw,
                Model:       modelName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenRouter JSON parse failed — returning raw text. Content: {Content}", raw);
            return new InteractionAnalysis("À vérifier", raw, "", "", raw, modelName);
        }
    }

    /// <summary>
    /// Collapses any model-supplied severity label to one of five canonical values.
    /// Accent- and case-insensitive; tolerates common synonyms ("CI", "interdit",
    /// "modéré", "majeur", …) so the DB never grows a fragmented vocabulary that
    /// would break filter-by-level lookups.
    /// </summary>
    private static string NormalizeLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "À vérifier";

        var key = AVCNDB.WPF.Helpers.NameNormalizer.Canonical(raw);

        // Contre-indiqué — absolute prohibition
        if (key.Contains("contre indique") || key.Contains("contre-indique")
            || key == "ci" || key.Contains("interdit") || key.Contains("majeur"))
            return "Contre-indiqué";

        // Déconseillé — avoid unless no alternative
        if (key.Contains("deconseille") || key.Contains("a eviter") || key.Contains("eviter"))
            return "Déconseillé";

        // Précaution — significant risk, dose adjustment / monitoring required
        if (key.Contains("precaution") || key.Contains("modere") || key.Contains("attention")
            || key.Contains("prudence"))
            return "Précaution";

        // A surveiller — mild interaction, monitor for symptoms
        if (key.Contains("surveiller") || key.Contains("surveillance") || key.Contains("mineur")
            || key.Contains("faible"))
            return "A surveiller";

        // Anything else (including blank) flagged for human review
        return "À vérifier";
    }
}
