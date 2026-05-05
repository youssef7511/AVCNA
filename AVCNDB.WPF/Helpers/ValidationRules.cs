using System.Text.RegularExpressions;

namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Reusable per-field validation rules with strict French error messages.
/// Each rule returns null when the value is valid, or a French error string otherwise.
/// </summary>
public static class ValidationRules
{
    private static readonly Regex BarcodeRegex = new(@"^\d{13}$", RegexOptions.Compiled);
    private static readonly Regex DigitsRegex = new(@"^\d+$", RegexOptions.Compiled);

    public static string? Required(string? value, string fieldLabel)
    {
        return string.IsNullOrWhiteSpace(value)
            ? $"Le champ « {fieldLabel} » est obligatoire."
            : null;
    }

    public static string? MaxLength(string? value, int max, string fieldLabel)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length > max
            ? $"Le champ « {fieldLabel} » ne peut pas dépasser {max} caractères (actuellement {value.Length})."
            : null;
    }

    public static string? MinLength(string? value, int min, string fieldLabel)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length < min
            ? $"Le champ « {fieldLabel} » doit contenir au moins {min} caractères."
            : null;
    }

    public static string? Barcode13(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return BarcodeRegex.IsMatch(value)
            ? null
            : "Le code-barre doit contenir exactement 13 chiffres.";
    }

    public static string? DigitsOnly(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DigitsRegex.IsMatch(value)
            ? null
            : $"Le champ « {fieldLabel} » ne doit contenir que des chiffres.";
    }

    public static string? NonNegative(int value, string fieldLabel)
    {
        return value < 0
            ? $"Le champ « {fieldLabel} » doit être positif ou nul."
            : null;
    }

    public static string? NonNegative(decimal value, string fieldLabel)
    {
        return value < 0
            ? $"Le champ « {fieldLabel} » doit être positif ou nul."
            : null;
    }

    public static string? InRange(int value, int min, int max, string fieldLabel)
    {
        return value < min || value > max
            ? $"Le champ « {fieldLabel} » doit être compris entre {min} et {max}."
            : null;
    }

    public static string? PercentRange(int value, string fieldLabel = "Taux remb. (%)")
    {
        return value < 0 || value > 100
            ? $"Le champ « {fieldLabel} » doit être entre 0 et 100."
            : null;
    }

    /// <summary>
    /// Cross-field rule: TARIF_REFERENCE should not exceed PRIX_PUBLIC.
    /// </summary>
    public static string? RefPriceNotAbovePpv(int refprice, int pamount)
    {
        return refprice > pamount
            ? "Le tarif de référence ne peut pas dépasser le PPV (Prix public)."
            : null;
    }
}
