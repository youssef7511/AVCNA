using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Converts a percentage (0–100) to a WPF PathGeometry representing an arc
/// drawn clockwise from the 12 o'clock position. Used for the donut chart.
/// The arc is centred at (60,60) with radius 50, matching a 120×120 canvas.
/// </summary>
public class PercentageToArcGeometryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double pct = value switch
        {
            double d => d,
            int    i => (double)i,
            float  f => (double)f,
            _        => 0
        };

        // Clamp – 100 % collapses an arc to nothing, handle as near-full
        pct = Math.Clamp(pct, 0, 99.99);

        const double cx = 60, cy = 60, r = 50;
        double angleDeg = pct / 100.0 * 360.0;
        double startRad = -Math.PI / 2;                       // 12 o'clock
        double endRad   = startRad + angleDeg * Math.PI / 180.0;

        var startPt = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
        var endPt   = new Point(cx + r * Math.Cos(endRad),   cy + r * Math.Sin(endRad));

        var arc    = new ArcSegment(endPt, new Size(r, r), 0, angleDeg > 180, SweepDirection.Clockwise, true);
        var figure = new PathFigure(startPt, new PathSegment[] { arc }, false);
        return new PathGeometry(new[] { figure });
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
