namespace AVCNDB.WPF.Contracts.Services;

public interface IMLPfeService
{
    /// <summary>Check if the Flask server is already listening on its configured port.</summary>
    Task<bool> IsRunningAsync();

    /// <summary>
    /// Start the Flask server if not already running, then open the browser.
    /// Returns an error message string on failure, null on success.
    /// </summary>
    Task<string?> LaunchAndOpenAsync();

    /// <summary>Base URL of the ML_pfe interface (e.g. http://127.0.0.1:5000).</summary>
    string BaseUrl { get; }
}
