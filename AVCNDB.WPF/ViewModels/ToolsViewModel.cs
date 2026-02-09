using System.Diagnostics;
using System.IO;
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

            var mysqldumpPath = FindMysqlDump();
            if (string.IsNullOrEmpty(mysqldumpPath))
            {
                await _dialogService.ShowErrorAsync("Erreur",
                    "mysqldump introuvable.\nVérifiez que MySQL est installé et que le chemin est dans le PATH système.");
                return;
            }

            var args = $"--host={parts.host} --port={parts.port} --user={parts.user} --password={parts.password} {parts.database}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mysqldumpPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                await File.WriteAllTextAsync(filePath, output);
                LastBackupInfo = $"Dernière sauvegarde: {DateTime.Now:dd/MM/yyyy HH:mm}";
                await _dialogService.ShowSuccessAsync("Sauvegarde réussie",
                    $"Base de données sauvegardée avec succès.\n{filePath}");
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur de sauvegarde",
                    $"La sauvegarde a échoué.\n{error}");
            }
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

            var mysqlPath = FindMysql();
            if (string.IsNullOrEmpty(mysqlPath))
            {
                await _dialogService.ShowErrorAsync("Erreur",
                    "mysql introuvable.\nVérifiez que MySQL est installé et que le chemin est dans le PATH système.");
                return;
            }

            var args = $"--host={parts.host} --port={parts.port} --user={parts.user} --password={parts.password} {parts.database}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mysqlPath,
                    Arguments = args,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var sqlContent = await File.ReadAllTextAsync(filePath);
            await process.StandardInput.WriteAsync(sqlContent);
            process.StandardInput.Close();

            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                await _dialogService.ShowSuccessAsync("Restauration réussie",
                    "La base de données a été restaurée avec succès.\nRedémarrez l'application pour appliquer les changements.");
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur de restauration",
                    $"La restauration a échoué.\n{error}");
            }
        }, "Restauration en cours...");
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
