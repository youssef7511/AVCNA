namespace AVCNDB.WPF.Contracts.Services;

public record InteractionAnalysis(
    string Level,
    string Description,
    string Mecanisme,
    string Conduite,
    string RawText,
    string Model);

public interface IOpenRouterService
{
    /// <summary>Identifier of the model in use (e.g. "nvidia/nemotron-3-super-120b-a12b").</summary>
    string ModelName { get; }

    Task<InteractionAnalysis> AnalyzeInteractionAsync(
        string dci1, string voie1,
        string dci2, string voie2,
        CancellationToken ct = default);
}
