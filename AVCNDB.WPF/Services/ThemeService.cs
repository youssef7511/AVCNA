using System.Windows;
using AVCNDB.WPF.Contracts.Services;
using MaterialDesignThemes.Wpf;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de gestion du thème (clair/sombre)
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;
    
    public event Action<AppTheme>? ThemeChanged;

    public void Initialize()
    {
        // Charger le thème depuis les paramètres
        var savedTheme = Properties.Settings.Default.Theme;
        if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
        {
            SetTheme(theme);
        }
        else
        {
            SetTheme(AppTheme.Light);
        }
    }

    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        
        var actualTheme = theme;
        if (theme == AppTheme.System)
        {
            actualTheme = IsSystemDarkMode() ? AppTheme.Dark : AppTheme.Light;
        }
        
        ApplyTheme(actualTheme == AppTheme.Dark);
        
        // Sauvegarder le choix
        Properties.Settings.Default.Theme = theme.ToString();
        Properties.Settings.Default.Save();
        
        ThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme switch
        {
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Light,
            AppTheme.System => AppTheme.Light,
            _ => AppTheme.Light
        };
        
        SetTheme(newTheme);
    }

    private void ApplyTheme(bool isDark)
    {
        var theme = _paletteHelper.GetTheme();
        
        if (isDark)
        {
            theme.SetBaseTheme(BaseTheme.Dark);
        }
        else
        {
            theme.SetBaseTheme(BaseTheme.Light);
        }
        
        _paletteHelper.SetTheme(theme);
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
