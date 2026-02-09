using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour les paramètres de l'application.
/// Lit / écrit la section ConnectionStrings de appsettings.json,
/// teste la connexion avec les valeurs saisies dans l'UI,
/// et persiste thème + alertes via Properties.Settings.Default.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly IConfiguration _configuration;

    // ── Apparence ──
    [ObservableProperty]
    private string _selectedTheme = "Clair";

    // ── Base de données ──
    [ObservableProperty]
    private string _databaseServer = "localhost";

    [ObservableProperty]
    private string _databasePort = "3306";

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _databaseUser = string.Empty;

    [ObservableProperty]
    private string _databasePassword = string.Empty;

    [ObservableProperty]
    private bool _connectionTested;

    [ObservableProperty]
    private bool _connectionSuccess;

    [ObservableProperty]
    private string _connectionMessage = string.Empty;

    // ── Alertes ──
    [ObservableProperty]
    private int _stockAlertThreshold;

    [ObservableProperty]
    private int _expiryAlertDays;

    // ── Infos ──
    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private string _databaseInfo = string.Empty;

    public SettingsViewModel(
        IThemeService themeService,
        IDialogService dialogService,
        IConfiguration configuration)
    {
        _themeService = themeService;
        _dialogService = dialogService;
        _configuration = configuration;

        LoadSettings();
    }

    // ──────────────────────────────────────────────
    // Chargement
    // ──────────────────────────────────────────────
    private void LoadSettings()
    {
        // Thème
        SelectedTheme = _themeService.CurrentTheme switch
        {
            AppTheme.Dark => "Sombre",
            AppTheme.System => "Système",
            _ => "Clair"
        };

        // Connexion : lire la VRAIE chaîne depuis appsettings.json
        var connStr = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        ParseConnectionString(connStr);

        // Alertes (stockées dans Properties.Settings car hors appsettings)
        StockAlertThreshold = Properties.Settings.Default.StockAlertThreshold;
        ExpiryAlertDays = Properties.Settings.Default.ExpiryAlertDays;

        // Version
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = $"{version?.Major}.{version?.Minor}.{version?.Build}";

        // Info
        DatabaseInfo = $"MySQL/MariaDB sur {DatabaseServer}:{DatabasePort}";

        ConnectionTested = false;
    }

    /// <summary>
    /// Décompose la chaîne de connexion MySQL en champs individuels.
    /// </summary>
    private void ParseConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            DatabaseServer = builder.Server;
            DatabasePort = builder.Port.ToString();
            DatabaseName = builder.Database;
            DatabaseUser = builder.UserID;
            DatabasePassword = builder.Password;
        }
        catch
        {
            // En cas de chaîne malformée, garder les valeurs par défaut
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        var theme = value switch
        {
            "Sombre" => AppTheme.Dark,
            "Système" => AppTheme.System,
            _ => AppTheme.Light
        };
        _themeService.SetTheme(theme);
    }

    // ──────────────────────────────────────────────
    // Tester la connexion avec les valeurs de l'UI
    // ──────────────────────────────────────────────
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionTested = false;
        ConnectionSuccess = false;

        await ExecuteAsync(async () =>
        {
            try
            {
                // Construire une NOUVELLE chaîne à partir des champs de l'UI
                var testConnStr = BuildConnectionString();

                await using var connection = new MySqlConnection(testConnStr);
                await connection.OpenAsync();

                ConnectionTested = true;
                ConnectionSuccess = true;
                ConnectionMessage = "Connexion réussie";
            }
            catch (Exception ex)
            {
                ConnectionTested = true;
                ConnectionSuccess = false;
                ConnectionMessage = $"Erreur : {ex.Message}";
            }
        }, "Test de connexion...");
    }

    /// <summary>
    /// Construit la chaîne de connexion MySQL à partir des valeurs de l'UI.
    /// </summary>
    private string BuildConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = DatabaseServer,
            Port = uint.TryParse(DatabasePort, out var p) ? p : 3306,
            Database = DatabaseName,
            UserID = DatabaseUser,
            Password = DatabasePassword,
            AllowZeroDateTime = true,
            ConvertZeroDateTime = true
        };
        return builder.ConnectionString;
    }

    // ──────────────────────────────────────────────
    // Enregistrer → écrit dans appsettings.json
    // ──────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveSettings()
    {
        try
        {
            // 1) Sauvegarder la connexion dans appsettings.json
            var newConnStr = BuildConnectionString();
            SaveConnectionStringToAppSettings(newConnStr);

            // 2) Sauvegarder alertes dans Properties.Settings
            Properties.Settings.Default.StockAlertThreshold = StockAlertThreshold;
            Properties.Settings.Default.ExpiryAlertDays = ExpiryAlertDays;
            Properties.Settings.Default.Save();

            DatabaseInfo = $"MySQL/MariaDB sur {DatabaseServer}:{DatabasePort}";

            await _dialogService.ShowSuccessAsync("Paramètres enregistrés",
                "Les paramètres ont été sauvegardés.\nRedémarrez l'application pour appliquer les changements de connexion.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erreur",
                $"Impossible de sauvegarder les paramètres :\n{ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour le fichier appsettings.json avec la nouvelle chaîne de connexion.
    /// </summary>
    private static void SaveConnectionStringToAppSettings(string newConnectionString)
    {
        var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        var json = File.ReadAllText(appSettingsPath);
        var doc = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })!;

        var connStrings = doc["ConnectionStrings"]?.AsObject();
        if (connStrings is not null)
        {
            connStrings["DefaultConnection"] = newConnectionString;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(appSettingsPath, doc.ToJsonString(options));
    }

    // ──────────────────────────────────────────────
    // Réinitialiser
    // ──────────────────────────────────────────────
    [RelayCommand]
    private async Task ResetSettings()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Réinitialiser les paramètres",
            "Voulez-vous vraiment réinitialiser tous les paramètres par défaut ?");

        if (confirm)
        {
            // Alertes
            Properties.Settings.Default.StockAlertThreshold = 10;
            Properties.Settings.Default.ExpiryAlertDays = 30;
            Properties.Settings.Default.Save();

            _themeService.SetTheme(AppTheme.Light);
            LoadSettings();

            await _dialogService.ShowSuccessAsync("Réinitialisé",
                "Les paramètres ont été réinitialisés.\nLes valeurs de connexion reflètent appsettings.json.");
        }
    }
}
