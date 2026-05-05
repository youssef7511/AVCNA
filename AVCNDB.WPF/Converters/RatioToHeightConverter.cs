using System.Globalization;
using System.Windows.Data;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Converts a 0.0–1.0 ratio to a pixel height.
/// ConverterParameter sets the maximum height (default 150).
/// </summary>
public class RatioToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double ratio = value is double d ? d : 0;
        double max = parameter is string s
            && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double p)
            ? p : 150.0;
        return Math.Max(3, ratio * max); // minimum 3px so a non-zero bar is always visible
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
