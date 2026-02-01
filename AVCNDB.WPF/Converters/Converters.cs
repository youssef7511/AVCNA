using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AVCNDB.WPF.Converters;

/// <summary>
/// Convertit un booléen en Visibility
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString()?.ToLower() == "invert";
            return (boolValue != invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            var invert = parameter?.ToString()?.ToLower() == "invert";
            return (visibility == Visibility.Visible) != invert;
        }
        return false;
    }
}

/// <summary>
/// Convertit null en Visibility
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.ToLower() == "invert";
        var isNull = value == null;
        return (isNull != invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit une chaîne vide en Visibility
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isEmpty = string.IsNullOrWhiteSpace(value?.ToString());
        var invert = parameter?.ToString()?.ToLower() == "invert";
        return (isEmpty != invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverse un booléen
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Convertit un nombre en Visibility (0 = Collapsed)
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            var invert = parameter?.ToString()?.ToLower() == "invert";
            return ((count > 0) != invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit une valeur décimale en format monétaire
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            var suffix = parameter?.ToString() ?? "DH";
            return $"{decimalValue:N2} {suffix}";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            var numericPart = new string(strValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numericPart, NumberStyles.Number, culture, out var result))
            {
                return result;
            }
        }
        return 0m;
    }
}

/// <summary>
/// Convertit une date en format relatif (il y a X jours)
/// </summary>
public class RelativeDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateValue)
        {
            var diff = DateTime.Now - dateValue;
            
            if (diff.TotalMinutes < 1)
                return "À l'instant";
            if (diff.TotalMinutes < 60)
                return $"Il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24)
                return $"Il y a {(int)diff.TotalHours} h";
            if (diff.TotalDays < 7)
                return $"Il y a {(int)diff.TotalDays} jour(s)";
            if (diff.TotalDays < 30)
                return $"Il y a {(int)(diff.TotalDays / 7)} semaine(s)";
            
            return dateValue.ToString("dd/MM/yyyy");
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un booléen en icône MaterialDesign (PackIcon Kind)
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var icons = parameter?.ToString()?.Split('|');
            if (icons?.Length == 2)
            {
                return boolValue ? icons[0] : icons[1];
            }
            return boolValue ? "Check" : "Close";
        }
        return "Help";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit deux valeurs en texte selon le booléen
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var texts = parameter?.ToString()?.Split('|');
            if (texts?.Length == 2)
            {
                return boolValue ? texts[0] : texts[1];
            }
            return boolValue ? "Oui" : "Non";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multiplie une valeur par un facteur
/// </summary>
public class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter != null)
        {
            if (double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            {
                return doubleValue * factor;
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un pourcentage en largeur de barre de progression
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int percentage && parameter is double maxWidth)
        {
            return (percentage / 100.0) * maxWidth;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
