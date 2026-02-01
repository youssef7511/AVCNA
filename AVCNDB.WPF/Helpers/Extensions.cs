namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Helper pour les extensions de chaînes de caractères
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Vérifie si une chaîne contient une autre chaîne (insensible à la casse)
    /// </summary>
    public static bool ContainsIgnoreCase(this string? source, string? value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            return false;
        
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retourne la chaîne ou une valeur par défaut si vide
    /// </summary>
    public static string OrDefault(this string? value, string defaultValue = "-")
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    /// <summary>
    /// Tronque une chaîne à une longueur maximale
    /// </summary>
    public static string Truncate(this string? value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Convertit la première lettre en majuscule
    /// </summary>
    public static string Capitalize(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return char.ToUpper(value[0]) + value[1..].ToLower();
    }

    /// <summary>
    /// Normalise les espaces (supprime les espaces multiples)
    /// </summary>
    public static string NormalizeSpaces(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return string.Join(" ", value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Helper pour les extensions décimales
/// </summary>
public static class DecimalExtensions
{
    /// <summary>
    /// Formate un prix en dirhams
    /// </summary>
    public static string ToPrice(this decimal? value, string currency = "DH")
    {
        return value.HasValue ? $"{value.Value:N2} {currency}" : "-";
    }

    /// <summary>
    /// Formate un pourcentage
    /// </summary>
    public static string ToPercentage(this int? value)
    {
        return value.HasValue ? $"{value.Value}%" : "-";
    }
}

/// <summary>
/// Helper pour les extensions de dates
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Vérifie si la date est dans le passé
    /// </summary>
    public static bool IsPast(this DateTime date)
    {
        return date < DateTime.Now;
    }

    /// <summary>
    /// Vérifie si la date expire bientôt (dans les X jours)
    /// </summary>
    public static bool IsExpiringSoon(this DateTime date, int days = 30)
    {
        return date <= DateTime.Now.AddDays(days) && date > DateTime.Now;
    }

    /// <summary>
    /// Retourne le nombre de jours restants
    /// </summary>
    public static int DaysRemaining(this DateTime date)
    {
        return (int)(date - DateTime.Now).TotalDays;
    }

    /// <summary>
    /// Formate une date en format français court
    /// </summary>
    public static string ToShortFrenchDate(this DateTime date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Formate une date en format relatif
    /// </summary>
    public static string ToRelativeDate(this DateTime date)
    {
        var diff = DateTime.Now - date;

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

        return date.ToShortFrenchDate();
    }
}
