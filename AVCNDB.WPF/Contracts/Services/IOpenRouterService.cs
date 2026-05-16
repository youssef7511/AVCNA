namespace AVCNDB.WPF.Contracts.Services;

public record InteractionAnalysis(
    string Level,
    string Description,
    string Mecanisme,
    string Conduite,
    string RawText);

public interface IOpenRouterService
{
    Task<InteractionAnalysis> AnalyzeInteractionAsync(
        string dci1, string voie1,
        string dci2, string voie2,
        CancellationToken ct = default);
}
