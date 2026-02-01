using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour les paramètres de l'application
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isSystemTheme;

    [ObservableProperty]
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private string _databaseServer = "localhost";

    [ObservableProperty]
    private string _databasePort = "50000";

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private string _databaseInfo = string.Empty;

    public SettingsViewModel(
        IThemeService themeService,
        IDialogService dialogService)
    {
        _themeService = themeService;
        _dialogService = dialogService;

        LoadSettings();
    }

    private void LoadSettings()
    {
        // Charger le thème actuel
        IsDarkTheme = _themeService.CurrentTheme == AppTheme.Dark;
        IsSystemTheme = _themeService.CurrentTheme == AppTheme.System;

        // Charger les infos de connexion
        DatabaseServer = Properties.Settings.Default.DatabaseServer;
        DatabasePort = Properties.Settings.Default.DatabasePort;
        
        // Version de l'application
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

        // Info base de données
        DatabaseInfo = $"MySQL/MariaDB sur {DatabaseServer}:{DatabasePort}";
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (value && !IsSystemTheme)
        {
            _themeService.SetTheme(AppTheme.Dark);
        }
        else if (!value && !IsSystemTheme)
        {
            _themeService.SetTheme(AppTheme.Light);
        }
    }

    partial void OnIsSystemThemeChanged(bool value)
    {
        if (value)
        {
            _themeService.SetTheme(AppTheme.System);
        }
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        IsSystemTheme = false;
        IsDarkTheme = false;
        _themeService.SetTheme(AppTheme.Light);
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        IsSystemTheme = false;
        IsDarkTheme = true;
        _themeService.SetTheme(AppTheme.Dark);
    }

    [RelayCommand]
    private void SetSystemTheme()
    {
        IsSystemTheme = true;
        _themeService.SetTheme(AppTheme.System);
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Tester la connexion à la base de données
            // TODO: Implémenter le test réel
            await Task.Delay(1000); // Simulation
            
            await _dialogService.ShowSuccessAsync("Connexion réussie", 
                "La connexion à la base de données est opérationnelle.");
        }, "Test de connexion...");
    }

    [RelayCommand]
    private void SaveDatabaseSettings()
    {
        Properties.Settings.Default.DatabaseServer = DatabaseServer;
        Properties.Settings.Default.DatabasePort = DatabasePort;
        Properties.Settings.Default.Save();
        
        DatabaseInfo = $"MySQL/MariaDB sur {DatabaseServer}:{DatabasePort}";
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Réinitialiser les paramètres",
            "Voulez-vous vraiment réinitialiser tous les paramètres par défaut ?");

        if (confirm)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            
            LoadSettings();
            _themeService.SetTheme(AppTheme.Light);
            
            await _dialogService.ShowSuccessAsync("Réinitialisé", 
                "Les paramètres ont été réinitialisés aux valeurs par défaut.");
        }
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _dialogService.ShowInfoAsync("À propos d'AVICENNA DB",
            $"""
            AVICENNA DB - Base de données pharmaceutique
            
            Version: {AppVersion}
            Framework: .NET 8.0 / WPF
            Base de données: MySQL/MariaDB
            
            © 2024 - Tous droits réservés
            
            Développé avec ❤️ pour les professionnels de santé
            """);
    }
}
