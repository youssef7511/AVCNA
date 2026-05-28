using System.Windows;
using System.Windows.Media;
using AVCNDB.WPF.Contracts.Services;
using MaterialDesignThemes.Wpf;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de gestion du thème (clair / sombre).
///
/// Stratégie : la palette « Apothecary Ledger » est définie comme un
/// ensemble de SolidColorBrush dans Application.Resources. Plutôt que
/// de remplacer le dictionnaire (ce qui invaliderait toutes les
/// références StaticResource), on mute la propriété Color de chaque
/// brush en place. Toutes les vues qui ont déjà résolu une référence
/// statique à un brush vont donc se repeindre automatiquement.
///
/// Cette stratégie cible aussi les surcharges Material Design
/// (MaterialDesignBackground, PrimaryHueMidBrush, etc.) déclarées
/// dans App.xaml et les arrêts des LinearGradientBrush.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public event Action<AppTheme>? ThemeChanged;

    public void Initialize()
    {
        var savedTheme = Properties.Settings.Default.Theme;
        if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
            SetTheme(theme);
        else
            SetTheme(AppTheme.Light);
    }

    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;

        var actual = theme;
        if (theme == AppTheme.System)
            actual = IsSystemDarkMode() ? AppTheme.Dark : AppTheme.Light;

        ApplyTheme(actual == AppTheme.Dark);

        Properties.Settings.Default.Theme = theme.ToString();
        Properties.Settings.Default.Save();

        ThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme switch
        {
            AppTheme.Light  => AppTheme.Dark,
            AppTheme.Dark   => AppTheme.Light,
            AppTheme.System => AppTheme.Light,
            _               => AppTheme.Light
        };
        SetTheme(newTheme);
    }

    // ─────────────────────────────────────────────────────────
    // PALETTE « APOTHECARY » — chaque clé a deux variantes
    // ─────────────────────────────────────────────────────────
    private static readonly Dictionary<string, (Color light, Color dark)> Palette = new()
    {
        // Surfaces
        ["PaperBrush"]          = (C("#F4EFE6"), C("#1A1714")),
        ["BoneBrush"]           = (C("#FBF8F2"), C("#24201C")),
        ["SurfaceBrush"]        = (C("#FBF8F2"), C("#24201C")),
        ["BackgroundBrush"]     = (C("#F4EFE6"), C("#1A1714")),
        ["CardBackgroundBrush"] = (C("#FBF8F2"), C("#24201C")),

        // Encre & texte
        ["InkBrush"]              = (C("#141312"), C("#EFE6D6")),
        ["InkSoftBrush"]          = (C("#5C544B"), C("#B0A695")),
        ["InkMuteBrush"]          = (C("#8A7F72"), C("#80766A")),
        ["TextPrimaryBrush"]      = (C("#141312"), C("#EFE6D6")),
        ["TextSecondaryBrush"]    = (C("#5C544B"), C("#B0A695")),
        ["TextOnPrimaryBrush"]    = (C("#FFFFFF"), C("#FFFFFF")),
        ["TextOnSecondaryBrush"]  = (C("#141312"), C("#1A1714")),

        // Hairlines : la Color change, l'Opacité reste réglée dans XAML
        ["HairlineBrush"]         = (C("#1F1A14"), C("#F0E6D6")),
        ["HairlineStrongBrush"]   = (C("#1F1A14"), C("#F0E6D6")),

        // Botanical (primaire)
        ["BotanicalBrush"]        = (C("#1E1E1E"), C("#333333")),
        ["BotanicalDarkBrush"]    = (C("#2D2D2D"), C("#555555")),
        ["BotanicalLightBrush"]   = (C("#EAEAEA"), C("#2D2D2D")),

        // Sidebar : reste sombre en clair ET en sombre — découplée de la marque
        ["SidebarBackgroundBrush"] = (C("#1E1E1E"), C("#1E1E1E")),

        // Info surface : panneaux d'aperçu / résultats IA
        ["InfoSurfaceBrush"]      = (C("#EBF0FB"), C("#1A2235")),

        // Vermillion (alertes)
        ["VermillionBrush"]       = (C("#C0362C"), C("#E55648")),
        ["VermillionDarkBrush"]   = (C("#7E1E18"), C("#B83C30")),
        ["VermillionLightBrush"]  = (C("#F4DAD6"), C("#3A1815")),

        // Ochre (charts, secondaire)
        ["OchreBrush"]            = (C("#B5853A"), C("#D49E5C")),
        ["OchreDarkBrush"]        = (C("#7A5821"), C("#B5853A")),
        ["OchreLightBrush"]       = (C("#F1E6D0"), C("#3A2C18")),

        // Moss (succès)
        ["MossBrush"]             = (C("#5A6B3B"), C("#8FA868")),
        ["MossDarkBrush"]         = (C("#3C4724"), C("#6F8848")),
        ["MossLightBrush"]        = (C("#E5E9D9"), C("#1F2A14")),

        // Indigo (info)
        ["IndigoBrush"]           = (C("#2C3E66"), C("#7B92C5")),
        ["IndigoLightBrush"]      = (C("#DCE2EE"), C("#1E2638")),

        // Anciennes clés Primary/Secondary/Accent (redirigées)
        ["PrimaryBrush"]       = (C("#1E1E1E"), C("#333333")),
        ["PrimaryDarkBrush"]   = (C("#2D2D2D"), C("#555555")),
        ["PrimaryLightBrush"]  = (C("#EAEAEA"), C("#2D2D2D")),
        ["SecondaryBrush"]      = (C("#B5853A"), C("#D49E5C")),
        ["SecondaryDarkBrush"]  = (C("#7A5821"), C("#B5853A")),
        ["SecondaryLightBrush"] = (C("#F1E6D0"), C("#3A2C18")),
        ["AccentBrush"]      = (C("#C0362C"), C("#E55648")),
        ["AccentLightBrush"] = (C("#F4DAD6"), C("#3A1815")),

        // Statuts (anciennes clés)
        ["SuccessBrush"]      = (C("#5A6B3B"), C("#8FA868")),
        ["SuccessLightBrush"] = (C("#E5E9D9"), C("#1F2A14")),
        ["SuccessDarkBrush"]  = (C("#3C4724"), C("#6F8848")),
        ["WarningBrush"]      = (C("#B5853A"), C("#D49E5C")),
        ["WarningLightBrush"] = (C("#F1E6D0"), C("#3A2C18")),
        ["WarningDarkBrush"]  = (C("#7A5821"), C("#B5853A")),
        ["ErrorBrush"]        = (C("#C0362C"), C("#E55648")),
        ["ErrorLightBrush"]   = (C("#F4DAD6"), C("#3A1815")),
        ["ErrorDarkBrush"]    = (C("#7E1E18"), C("#B83C30")),
        ["InfoBrush"]         = (C("#2C3E66"), C("#7B92C5")),
        ["InfoLightBrush"]    = (C("#DCE2EE"), C("#1E2638")),
        ["InfoDarkBrush"]     = (C("#1B264A"), C("#4D6098")),

        // Bordures
        ["BorderBrush"]      = (C("#D9CFBF"), C("#3A332D")),
        ["BorderLightBrush"] = (C("#EAE2D2"), C("#2A2520")),
        ["BorderDarkBrush"]  = (C("#A99A85"), C("#5A5048")),

        // Médicaments (encrés)
        ["MedicGenericBrush"]    = (C("#5A6B3B"), C("#8FA868")),
        ["MedicBrandBrush"]      = (C("#2C3E66"), C("#7B92C5")),
        ["MedicPediatricBrush"]  = (C("#7A5821"), C("#D49E5C")),
        ["MedicControlledBrush"] = (C("#7E1E18"), C("#E55648")),

        // ── Surcharges Material Design ──────────────────────
        ["MaterialDesignBackground"]     = (C("#F4EFE6"), C("#1A1714")),
        ["MaterialDesignPaper"]          = (C("#FBF8F2"), C("#24201C")),
        ["MaterialDesignCardBackground"] = (C("#FBF8F2"), C("#24201C")),
        ["MaterialDesignBody"]           = (C("#141312"), C("#EFE6D6")),
        ["MaterialDesignBodyLight"]      = (C("#5C544B"), C("#B0A695")),
        ["MaterialDesignDivider"]        = (C("#D9CFBF"), C("#3A332D")),
        ["MaterialDesignToolForeground"] = (C("#5C544B"), C("#B0A695")),
        ["MaterialDesignToolBackground"] = (C("#FBF8F2"), C("#24201C")),
        ["MaterialDesignSelection"]      = (C("#EAEAEA"), C("#2D2D2D")),
        ["MaterialDesignFlatButtonClick"] = (C("#EAEAEA"), C("#2D2D2D")),

        // Material : palette primaire / secondaire
        ["PrimaryHueLightBrush"]           = (C("#EAEAEA"), C("#2D2D2D")),
        ["PrimaryHueLightForegroundBrush"] = (C("#141312"), C("#EFE6D6")),
        ["PrimaryHueMidBrush"]             = (C("#1E1E1E"), C("#333333")),
        ["PrimaryHueMidForegroundBrush"]   = (C("#FFFFFF"), C("#FFFFFF")),
        ["PrimaryHueDarkBrush"]            = (C("#2D2D2D"), C("#555555")),
        ["PrimaryHueDarkForegroundBrush"]  = (C("#FFFFFF"), C("#FFFFFF")),

        ["SecondaryHueLightBrush"]           = (C("#F1E6D0"), C("#3A2C18")),
        ["SecondaryHueLightForegroundBrush"] = (C("#141312"), C("#EFE6D6")),
        ["SecondaryHueMidBrush"]             = (C("#B5853A"), C("#D49E5C")),
        ["SecondaryHueMidForegroundBrush"]   = (C("#FBF8F2"), C("#1A1714")),
        ["SecondaryHueDarkBrush"]            = (C("#7A5821"), C("#B5853A")),
        ["SecondaryHueDarkForegroundBrush"]  = (C("#FBF8F2"), C("#1A1714")),

        ["SecondaryAccentBrush"]           = (C("#B5853A"), C("#D49E5C")),
        ["SecondaryAccentForegroundBrush"] = (C("#FBF8F2"), C("#1A1714")),

        ["ValidationErrorBrush"]              = (C("#C0362C"), C("#E55648")),
        ["MaterialDesignValidationErrorBrush"] = (C("#C0362C"), C("#E55648")),
    };

    // ─────────────────────────────────────────────────────────
    // Gradients : on remplace les arrêts à chaque commutation
    // ─────────────────────────────────────────────────────────
    private static readonly Dictionary<string, (Color[] light, Color[] dark)> Gradients = new()
    {
        // Primary : foncé → encore plus foncé (light) | clair → médian (dark)
        ["PrimaryGradientBrush"] = (
            new[] { C("#1E1E1E"), C("#2D2D2D") },
            new[] { C("#333333"), C("#4D4D4D") }
        ),
        ["SecondaryGradientBrush"] = (
            new[] { C("#B5853A"), C("#7A5821") },
            new[] { C("#D49E5C"), C("#B5853A") }
        ),
        // Header gradient : 3 stops, symétriques
        ["HeaderGradientBrush"] = (
            new[] { C("#2D2D2D"), C("#1E1E1E"), C("#2D2D2D") },
            new[] { C("#555555"), C("#333333"), C("#555555") }
        ),
    };

    private void ApplyTheme(bool isDark)
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        // 1) Aligne d'ABORD le BaseTheme Material (pour scrollbars, ripple…).
        //    On le fait en premier car PaletteHelper.SetTheme injecte ses
        //    propres brushes dans Application.Resources, ce qui peut écraser
        //    nos clés (MaterialDesignPaper, MaterialDesignBody…). Nos
        //    mutations doivent donc être appliquées APRÈS.
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);

        // 2) Mute la Color de chaque SolidColorBrush in place. Si une clé a
        //    été remplacée par PaletteHelper (instance différente), on
        //    remplace l'entrée dans Application.Resources par un nouveau
        //    brush portant la bonne couleur — ainsi les DynamicResource
        //    consommatrices repaintent.
        foreach (var (key, (light, dark)) in Palette)
        {
            var target = isDark ? dark : light;
            if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = target;
            }
            else
            {
                // Brush absent ou frozen → on remplace l'entrée
                resources[key] = new SolidColorBrush(target);
            }
        }

        // 3) Gradients : remplace les Color des GradientStops dans l'ordre.
        foreach (var (key, (light, dark)) in Gradients)
        {
            var palette = isDark ? dark : light;
            if (resources[key] is LinearGradientBrush g && !g.IsFrozen)
            {
                for (int i = 0; i < g.GradientStops.Count && i < palette.Length; i++)
                {
                    g.GradientStops[i].Color = palette[i];
                }
            }
            else
            {
                // Reconstruit le gradient si nécessaire
                var newG = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 1) };
                for (int i = 0; i < palette.Length; i++)
                    newG.GradientStops.Add(new GradientStop(palette[i], i / (double)Math.Max(1, palette.Length - 1)));
                resources[key] = newG;
            }
        }
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

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
