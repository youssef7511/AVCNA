using System.Globalization;
using System.Windows.Data;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Two-way converter: int millièmes ↔ decimal string (e.g., 3585 → "3.585").
/// Used for editable price columns in DataGrid.
/// </summary>
public class MillimesToDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal)
            return (intVal / 1000.0).ToString("F3", CultureInfo.InvariantCulture);
        return "0.000";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            str = str.Replace(',', '.').Trim();
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                return (int)Math.Round(dec * 1000);
        }
        return 0;
    }
}
