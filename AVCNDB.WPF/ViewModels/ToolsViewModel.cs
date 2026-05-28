using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using Microsoft.Extensions.Configuration;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la page Outils
/// Fournit les commandes pour les utilitaires: export, import, PDF, sauvegarde, restauration, statistiques
/// </summary>
public partial class ToolsViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _medicRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly IRepository<Formes> _formesRepository;
    private readonly IRepository<Voies> _voiesRepository;
    private readonly IExcelService _excelService;
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly IConfiguration _configuration;
    private const string DockerDbContainerName = "avcndb-db";

    // ============================================
    // STATISTIQUES
    // ============================================
    [ObservableProperty]
    private int _totalMedics;

    [ObservableProperty]
    private int _totalDcis;

    [ObservableProperty]
    private int _totalFamilies;

    [ObservableProperty]
    private int _totalLabos;

    [ObservableProperty]
    private int _totalFormes;

    [ObservableProperty]
    private int _totalVoies;

    [ObservableProperty]
    private bool _showStats;

    [ObservableProperty]
    private string _lastBackupInfo = string.Empty;

    public ToolsViewModel(
        IRepository<Medic> medicRepository,
        IRepository<Dci> dciRepository,
        IRepository<Families> familyRepository,
        IRepository<Labos> laboRepository,
        IRepository<Formes> formesRepository,
        IRepository<Voies> voiesRepository,
        IExcelService excelService,
        IPdfService pdfService,
        IDialogService dialogService,
        IConfiguration configuration)
    {
        _medicRepository = medicRepository;
        _dciRepository = dciRepository;
        _familyRepository = familyRepository;
        _laboRepository = laboRepository;
        _formesRepository = formesRepository;
        _voiesRepository = voiesRepository;
        _excelService = excelService;
        _pdfService = pdfService;
        _dialogService = dialogService;
        _configuration = configuration;
    }

    // ============================================
    // EXPORT EXCEL GLOBAL
    // ============================================
    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"AVCNDB_Export_{DateTime.Now:yyyyMMdd_HHmmss}",
            "Exporter toutes les données vers Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var medics = await _medicRepository.GetAllAsync();
            await _excelService.ExportAsync(medics, filePath, "Médicaments");
            await _dialogService.ShowSuccessAsync("Export réussi",
                $"Toutes les données médicaments exportées.\n{medics.Count()} enregistrements.\n{filePath}");
        }, "Export Excel en cours...");
    }

    // ============================================
    // IMPORT EXCEL GLOBAL
    // ============================================
    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Excel Files|*.xlsx;*.xls",
            "Importer des données depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirmer l'import",
            "L'import va ajouter de nouveaux enregistrements à la base de données.\nLes doublons seront ignorés.\n\nContinuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var imported = await _excelService.ImportAsync<Medic>(filePath, "Médicaments");
            var count = 0;
            foreach (var item in imported)
            {
                await _medicRepository.AddAsync(item);
                count++;
            }

            await _dialogService.ShowSuccessAsync("Import terminé",
                $"{count} enregistrement(s) importé(s) avec succès.");
        }, "Import Excel en cours...");
    }

    // ============================================
    // RAPPORT PDF GLOBAL
    // ============================================
    [RelayCommand]
    private async Task GeneratePdfAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            $"AVCNDB_Rapport_{DateTime.Now:yyyyMMdd_HHmmss}",
            "Générer un rapport PDF");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var allMedics = await _medicRepository.GetAllAsync();
            await _pdfService.GenerateCustomReportAsync(allMedics, "Base de Données Médicamenteuse - Rapport Complet", filePath);
            await _dialogService.ShowSuccessAsync("Rapport généré",
                $"Rapport PDF créé avec {allMedics.Count()} médicaments.\n{filePath}");
        }, "Génération du rapport PDF...");
    }

    // ============================================
    // SAUVEGARDE BASE DE DONNÉES
    // ============================================
    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "SQL Files|*.sql",
            $"AVCNDB_Backup_{DateTime.Now:yyyyMMdd_HHmmss}",
            "Sauvegarder la base de données");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var parts = ParseConnectionString(connectionString);

            var localBackup = await TryBackupWithLocalClientAsync(parts, filePath);
            if (!localBackup.Success)
            {
                var dockerBackup = await TryBackupWithDockerAsync(parts, filePath);
                if (!dockerBackup.Success)
                {
                    await _dialogService.ShowErrorAsync("Erreur de sauvegarde",
                        $"La sauvegarde a échoué.\n\nLocal:\n{localBackup.Error}\n\nDocker:\n{dockerBackup.Error}");
                    return;
                }
            }

            LastBackupInfo = $"Dernière sauvegarde: {DateTime.Now:dd/MM/yyyy HH:mm}";
            await _dialogService.ShowSuccessAsync("Sauvegarde réussie",
                $"Base de données sauvegardée avec succès.\n{filePath}");
        }, "Sauvegarde en cours...");
    }

    // ============================================
    // RESTAURATION BASE DE DONNÉES
    // ============================================
    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "SQL Files|*.sql",
            "Restaurer depuis une sauvegarde");

        if (string.IsNullOrEmpty(filePath)) return;

        var confirm = await _dialogService.ShowConfirmAsync(
            "⚠️ Confirmation Restauration",
            "ATTENTION : La restauration va REMPLACER toutes les données actuelles de la base de données.\n\n" +
            "Cette opération est irréversible.\n\n" +
            "Êtes-vous sûr de vouloir continuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var parts = ParseConnectionString(connectionString);
            var sqlContent = await File.ReadAllTextAsync(filePath);

            var localRestore = await TryRestoreWithLocalClientAsync(parts, sqlContent);
            if (!localRestore.Success)
            {
                var dockerRestore = await TryRestoreWithDockerAsync(parts, sqlContent);
                if (!dockerRestore.Success)
                {
                    await _dialogService.ShowErrorAsync("Erreur de restauration",
                        $"La restauration a échoué.\n\nLocal:\n{localRestore.Error}\n\nDocker:\n{dockerRestore.Error}");
                    return;
                }
            }

            await _dialogService.ShowSuccessAsync("Restauration réussie",
                "La base de données a été restaurée avec succès.\nRedémarrez l'application pour appliquer les changements.");
        }, "Restauration en cours...");
    }

    // ============================================
    // RÉENTRAÎNEMENT DU MODÈLE IA (ML_pfe)
    // ============================================
    /// <summary>
    /// Confirme avec l'utilisateur puis POST /admin/retrain sur le service
    /// Flask ML_pfe. Renvoie une boîte de dialogue succès/erreur avec le
    /// nombre de lignes utilisées et le score F1 retourné par l'endpoint.
    /// </summary>
    [RelayCommand]
    private async Task RetrainMlPfeAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Réentraîner le modèle IA",
            "Cette opération va reconstruire le modèle des contre-indications " +
            "à partir des interactions actuellement approuvées dans la base.\n\n" +
            "L'opération prend généralement quelques secondes. Continuer ?");
        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            var baseUrl = _configuration["MlPfe:BaseUrl"] ?? $"http://127.0.0.1:{_configuration["MlPfe:Port"] ?? "5000"}";
            var token   = _configuration["MlPfe:RetrainToken"] ?? "change-me";
            var url     = baseUrl.TrimEnd('/') + "/admin/retrain";

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var req  = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                resp = await http.SendAsync(req);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    "Service ML_pfe injoignable",
                    $"Impossible de contacter {url}.\n\n" +
                    $"Détails : {ex.Message}\n\n" +
                    "Vérifiez que le service Flask est démarré "
                    + "(python app.py dans le dossier ML_pfe).");
                return;
            }

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                await _dialogService.ShowErrorAsync(
                    "Échec du réentraînement",
                    $"Le service a répondu {(int)resp.StatusCode} {resp.StatusCode}.\n\n{body}");
                return;
            }

            // Parse the JSON { ok: true, metrics: { rows_positive, rows_negative, dci_count,
            //                                       macro_f1_train, macro_f1_cv, ... } }
            int  positives = 0;
            int  negatives = 0;
            int  dciCount  = 0;
            double? macroF1Cv = null;
            double? macroF1Test = null;
            double  macroF1Train = 0;
            string negativeLabel = "Aucune interaction connue";
            var warnings = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var metrics  = doc.RootElement.GetProperty("metrics");
                positives    = metrics.GetProperty("rows_positive").GetInt32();
                negatives    = metrics.GetProperty("rows_negative").GetInt32();
                dciCount     = metrics.GetProperty("dci_count").GetInt32();
                macroF1Train = metrics.GetProperty("macro_f1_train").GetDouble();
                if (metrics.TryGetProperty("macro_f1_cv", out var cv) && cv.ValueKind == JsonValueKind.Number)
                    macroF1Cv = cv.GetDouble();
                if (metrics.TryGetProperty("macro_f1_test", out var test) && test.ValueKind == JsonValueKind.Number)
                    macroF1Test = test.GetDouble();
                if (metrics.TryGetProperty("negative_label", out var neg) && neg.ValueKind == JsonValueKind.String)
                    negativeLabel = neg.GetString() ?? negativeLabel;
                if (metrics.TryGetProperty("warnings", out var warns) && warns.ValueKind == JsonValueKind.Array)
                {
                    foreach (var warning in warns.EnumerateArray())
                    {
                        if (warning.ValueKind == JsonValueKind.String)
                            warnings.Add(warning.GetString() ?? "");
                    }
                }
            }
            catch
            {
                // The endpoint returned an unexpected shape — surface the raw body.
                await _dialogService.ShowSuccessAsync("Modèle réentraîné", body);
                return;
            }

            var cvLine = macroF1Cv.HasValue
                ? $"\nMacro F1 (CV 5-fold) : {macroF1Cv.Value:F3}"
                : "";
            var testLine = macroF1Test.HasValue
                ? $"\nMacro F1 (test) : {macroF1Test.Value:F3}"
                : "";
            var warningLine = warnings.Count > 0
                ? "\n\nAvertissement(s) :\n- " + string.Join("\n- ", warnings.Where(w => !string.IsNullOrWhiteSpace(w)))
                : "";
            await _dialogService.ShowSuccessAsync(
                "Modèle réentraîné",
                $"Modèle reconstruit à partir de {positives} interaction(s) approuvée(s)\n" +
                $"+ {negatives} paire(s) tirée(s) au sort comme \"{negativeLabel}\".\n\n" +
                $"DCIs connues : {dciCount}\n" +
                $"Macro F1 (train - contrôle technique) : {macroF1Train:F3}" + cvLine + testLine + warningLine);
        }, "Réentraînement du modèle IA...");
    }

    // ============================================
    // STATISTIQUES
    // ============================================
    [RelayCommand]
    private async Task ShowStatsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var medics = await _medicRepository.GetAllAsync();
            var dcis = await _dciRepository.GetAllAsync();
            var families = await _familyRepository.GetAllAsync();
            var labos = await _laboRepository.GetAllAsync();
            var formes = await _formesRepository.GetAllAsync();
            var voies = await _voiesRepository.GetAllAsync();

            TotalMedics = medics.Count();
            TotalDcis = dcis.Count();
            TotalFamilies = families.Count();
            TotalLabos = labos.Count();
            TotalFormes = formes.Count();
            TotalVoies = voies.Count();

            ShowStats = true;
        }, "Chargement des statistiques...");
    }

    // ============================================
    // HELPERS
    // ============================================
    private static async Task<(bool Success, string Error)> TryBackupWithLocalClientAsync(
        (string host, string port, string user, string password, string database) parts,
        string filePath)
    {
        var mysqldumpPath = FindMysqlDump();
        if (string.IsNullOrEmpty(mysqldumpPath))
        {
            return (false, "mysqldump introuvable (installation locale/PATH).");
        }

        try
        {
            var args = $"--host={parts.host} --port={parts.port} --user={parts.user} --password={parts.password} {parts.database}";
            var result = await RunProcessAsync(mysqldumpPath, args, null);

            if (result.ExitCode != 0)
            {
                return (false, result.StandardError.Trim());
            }

            await File.WriteAllTextAsync(filePath, result.StandardOutput);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<(bool Success, string Error)> TryBackupWithDockerAsync(
        (string host, string port, string user, string password, string database) parts,
        string filePath)
    {
        var running = await IsDockerContainerRunningAsync(DockerDbContainerName);
        if (!running)
        {
            return (false, $"Conteneur Docker '{DockerDbContainerName}' non démarré.");
        }

        try
        {
            var dumpArgs = $"exec {DockerDbContainerName} mariadb-dump --user={parts.user} --password={parts.password} --databases {parts.database}";
            var dumpResult = await RunProcessAsync("docker", dumpArgs, null);

            if (dumpResult.ExitCode != 0)
            {
                // Fallback for images exposing mysqldump instead of mariadb-dump.
                dumpArgs = $"exec {DockerDbContainerName} mysqldump --user={parts.user} --password={parts.password} --databases {parts.database}";
                dumpResult = await RunProcessAsync("docker", dumpArgs, null);
            }

            if (dumpResult.ExitCode != 0)
            {
                return (false, dumpResult.StandardError.Trim());
            }

            await File.WriteAllTextAsync(filePath, dumpResult.StandardOutput);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<(bool Success, string Error)> TryRestoreWithLocalClientAsync(
        (string host, string port, string user, string password, string database) parts,
        string sqlContent)
    {
        var mysqlPath = FindMysql();
        if (string.IsNullOrEmpty(mysqlPath))
        {
            return (false, "mysql introuvable (installation locale/PATH).");
        }

        try
        {
            var args = $"--host={parts.host} --port={parts.port} --user={parts.user} --password={parts.password} {parts.database}";
            var result = await RunProcessAsync(mysqlPath, args, sqlContent);
            return result.ExitCode == 0
                ? (true, string.Empty)
                : (false, result.StandardError.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<(bool Success, string Error)> TryRestoreWithDockerAsync(
        (string host, string port, string user, string password, string database) parts,
        string sqlContent)
    {
        var running = await IsDockerContainerRunningAsync(DockerDbContainerName);
        if (!running)
        {
            return (false, $"Conteneur Docker '{DockerDbContainerName}' non démarré.");
        }

        try
        {
            var restoreArgs = $"exec -i {DockerDbContainerName} mariadb --user={parts.user} --password={parts.password} {parts.database}";
            var restoreResult = await RunProcessAsync("docker", restoreArgs, sqlContent);

            if (restoreResult.ExitCode != 0)
            {
                // Fallback for images exposing mysql client.
                restoreArgs = $"exec -i {DockerDbContainerName} mysql --user={parts.user} --password={parts.password} {parts.database}";
                restoreResult = await RunProcessAsync("docker", restoreArgs, sqlContent);
            }

            return restoreResult.ExitCode == 0
                ? (true, string.Empty)
                : (false, restoreResult.StandardError.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<bool> IsDockerContainerRunningAsync(string containerName)
    {
        try
        {
            var inspectArgs = $"inspect -f \"{{{{.State.Running}}}}\" {containerName}";
            var result = await RunProcessAsync("docker", inspectArgs, null);
            return result.ExitCode == 0 &&
                   result.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(
        string fileName,
        string arguments,
        string? standardInput)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = standardInput is not null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }

    private static string? FindMysqlDump()
    {
        var paths = new[]
        {
            @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 8.4\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 9.0\bin\mysqldump.exe",
            @"C:\xampp\mysql\bin\mysqldump.exe",
            @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysqldump.exe",
            "mysqldump"  // PATH lookup
        };

        foreach (var path in paths)
        {
            if (path == "mysqldump" || File.Exists(path))
                return path;
        }
        return null;
    }

    private static string? FindMysql()
    {
        var paths = new[]
        {
            @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
            @"C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe",
            @"C:\Program Files\MySQL\MySQL Server 9.0\bin\mysql.exe",
            @"C:\xampp\mysql\bin\mysql.exe",
            @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysql.exe",
            "mysql"  // PATH lookup
        };

        foreach (var path in paths)
        {
            if (path == "mysql" || File.Exists(path))
                return path;
        }
        return null;
    }

    private static (string host, string port, string user, string password, string database) ParseConnectionString(string connectionString)
    {
        var dict = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());

        return (
            host: dict.GetValueOrDefault("server", "127.0.0.1"),
            port: dict.GetValueOrDefault("port", "3306"),
            user: dict.GetValueOrDefault("user id", "root"),
            password: dict.GetValueOrDefault("password", ""),
            database: dict.GetValueOrDefault("database", "MEDICDB")
        );
    }
}
