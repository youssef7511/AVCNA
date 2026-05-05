using System.Globalization;
using System.Windows.Data;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// Converts a similarity score (int) to "High" (>=80), "Medium" (>=60), or "Low" (&lt;60)
/// for DataTrigger-based color coding.
/// </summary>
public class ScoreThresholdConverter : IValueConverter
{
    public static readonly ScoreThresholdConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score)
        {
            return score >= 80 ? "High" : score >= 60 ? "Medium" : "Low";
        }
        return "Low";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
