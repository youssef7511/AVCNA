using System.Collections;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Converts an IEnumerable&lt;MonthBarItem&gt; into a PointCollection sized to a
/// "width,height" parameter, used to draw the dashboard hero sparkline.
/// Origin (0,0) is top-left; lower ratios produce higher Y values so the
/// sparkline visually matches the bar chart.
/// </summary>
public class MonthsToPolylineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var points = new PointCollection();
        if (value is not IEnumerable items) return points;

        // parameter: "W,H"
        double w = 220, h = 56;
        if (parameter is string s)
        {
            var parts = s.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var pw) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var ph))
            {
                w = pw; h = ph;
            }
        }

        var list = items.Cast<object>().OfType<MonthBarItem>().ToList();
        if (list.Count == 0) return points;

        double step = list.Count > 1 ? w / (list.Count - 1) : 0;
        for (int i = 0; i < list.Count; i++)
        {
            double r = Math.Clamp(list[i].Ratio, 0, 1);
            double x = i * step;
            double y = (1.0 - r) * h;
            points.Add(new System.Windows.Point(x, y));
        }
        return points;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
