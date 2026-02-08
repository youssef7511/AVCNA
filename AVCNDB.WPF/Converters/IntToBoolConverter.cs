using System.Globalization;
using System.Windows.Data;

namespace AVCNDB.WPF.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i != 0,
            long l => l != 0,
            short s => s != 0,
            byte b => b != 0,
            bool b => b,
            _ => false
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? 1 : 0;
        }

        return 0;
    }
}
