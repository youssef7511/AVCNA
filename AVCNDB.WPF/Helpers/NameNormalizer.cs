using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Computes a canonical key for reference-by-name fields (DCI, Labo, Famille, …).
/// The canonical form is lowercased, diacritic-stripped, whitespace-collapsed.
/// Two strings sharing the same canonical key are considered the same business value
/// regardless of casing, accents, or extra whitespace.
///
/// Used to detect duplicates in reference tables (e.g. "PARACETAMOL", "Paracétamol",
/// "paracetamol " all canonicalize to "paracetamol").
/// </summary>
public static class NameNormalizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Canonical(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var lowered = raw.Trim().ToLowerInvariant();
        var decomposed = lowered.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var recomposed = sb.ToString().Normalize(NormalizationForm.FormC);
        return WhitespaceRegex.Replace(recomposed, " ").Trim();
    }

    /// <summary>True if two strings are canonically equal.</summary>
    public static bool AreSame(string? a, string? b) => Canonical(a) == Canonical(b);
}
