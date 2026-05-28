using System.Diagnostics;
using System.IO;
using System.Net.Http;
using AVCNDB.WPF.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AVCNDB.WPF.Services;

public class MLPfeService : IMLPfeService
{
    private readonly string _workingDirectory;
    private readonly string _pythonExe;
    private readonly int _port;
    private readonly HttpClient _http;

    public string BaseUrl => $"http://127.0.0.1:{_port}";

    public MLPfeService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        var configuredWorkingDirectory = configuration["MlPfe:WorkingDirectory"];
        _workingDirectory = string.IsNullOrWhiteSpace(configuredWorkingDirectory)
            ? Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\ML_pfe")
            : configuredWorkingDirectory;
        _pythonExe = configuration["MlPfe:PythonExe"] ?? "python";
        _port = int.TryParse(configuration["MlPfe:Port"], out var p) ? p : 5000;
    }

    public async Task<bool> IsRunningAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var resp = await _http.GetAsync(BaseUrl, cts.Token);
            return true; // any HTTP response means Flask is up
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> LaunchAndOpenAsync()
    {
        // If already running, just open the browser
        if (await IsRunningAsync())
        {
            OpenBrowser(BaseUrl);
            return null;
        }

        // Resolve and validate the working directory
        var dir = Path.GetFullPath(
            Path.IsPathRooted(_workingDirectory)
                ? _workingDirectory
                : Path.Combine(AppContext.BaseDirectory, _workingDirectory));
        if (!Directory.Exists(dir))
            return $"Dossier ML_pfe introuvable :\n{dir}\n\nVérifiez MlPfe:WorkingDirectory dans appsettings.json.";

        var appPy = Path.Combine(dir, "app.py");
        if (!File.Exists(appPy))
            return $"app.py introuvable dans :\n{dir}";

        // Start Flask in a visible console window so the user can see its logs
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName  = "cmd.exe",
                Arguments = $"/k \"{_pythonExe}\" app.py",
                WorkingDirectory = dir,
                UseShellExecute  = true,
            };
            Process.Start(psi);
            Log.Information("MLPfe: started Flask in {Dir}", dir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MLPfe: failed to start Flask");
            return $"Impossible de démarrer Flask :\n{ex.Message}\n\nAssurez-vous que Python est installé et accessible via '{_pythonExe}'.";
        }

        // Wait up to 12 s for Flask to become responsive.
        // After the first successful ping, wait one extra second so the server
        // fully stabilizes before the browser connects.
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(700);
            if (await IsRunningAsync())
            {
                await Task.Delay(1000); // absorb any remaining startup lag
                OpenBrowser(BaseUrl);
                return null;
            }
        }

        // Timeout — open browser anyway; user can reload once it's up
        OpenBrowser(BaseUrl);
        return null;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "MLPfe: could not open browser for {Url}", url);
        }
    }
}
