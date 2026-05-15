using System.Globalization;
using System.Windows.Data;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Maps level strings emitted by the local `interact` table OR by the
/// OpenRouter / Nemotron analysis onto a single severity key so that
/// DataTriggers in XAML can drive consistent badge / bar colors.
///
/// Local vocabulary: Contre-indication, Association déconseillée, Précaution d'emploi.
/// AI vocabulary:    Contre-indiqué, Déconseillé, Précaution, A surveiller.
/// </summary>
public class LevelToSeverityKeyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var raw = value as string;
        if (string.IsNullOrWhiteSpace(raw)) return "unknown";

        // Case- and diacritic-insensitive match on the leading prefix so we
        // tolerate small wording drifts ("À surveiller" vs "A surveiller").
        var normalised = raw.Trim();
        if (normalised.StartsWith("Contre-ind",     StringComparison.OrdinalIgnoreCase)) return "critical";
        if (normalised.StartsWith("Association déc",StringComparison.OrdinalIgnoreCase)) return "discouraged";
        if (normalised.StartsWith("Déconseillé",    StringComparison.OrdinalIgnoreCase)) return "discouraged";
        if (normalised.StartsWith("Précaution",     StringComparison.OrdinalIgnoreCase)) return "caution";
        if (normalised.StartsWith("A surveiller",   StringComparison.OrdinalIgnoreCase)) return "monitor";
        if (normalised.StartsWith("À surveiller",   StringComparison.OrdinalIgnoreCase)) return "monitor";
        return "unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
