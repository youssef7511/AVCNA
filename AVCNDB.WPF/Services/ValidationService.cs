using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de validation des entités
/// </summary>
public partial class ValidationService : IValidationService
{
    public ValidationResult<T> Validate<T>(T entity) where T : class
    {
        var result = new ValidationResult<T> { IsValid = true, Entity = entity };
        var validationContext = new ValidationContext(entity);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(entity, validationContext, validationResults, true))
        {
            result.IsValid = false;
            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    result.AddError(memberName, validationResult.ErrorMessage ?? "Erreur de validation");
                }
            }
        }

        return result;
    }

    public IEnumerable<string> ValidateProperty(object? value, string propertyName, 
        IEnumerable<ValidationAttribute> validationAttributes)
    {
        var errors = new List<string>();
        var validationContext = new ValidationContext(new object()) { MemberName = propertyName };

        foreach (var attribute in validationAttributes)
        {
            var validationResult = attribute.GetValidationResult(value, validationContext);
            if (validationResult != null && validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                errors.Add(validationResult.ErrorMessage ?? "Erreur de validation");
            }
        }

        return errors;
    }

    public bool IsValidBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        
        // EAN-13 ou Code 128
        return BarcodeRegex().IsMatch(barcode);
    }

    public bool IsValidAmm(string amm)
    {
        if (string.IsNullOrWhiteSpace(amm)) return true; // AMM optionnel
        
        // Format AMM tunisien : lettres + chiffres
        return AmmRegex().IsMatch(amm);
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true; // Email optionnel
        
        return EmailRegex().IsMatch(email);
    }

    public bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true; // Téléphone optionnel
        
        // Format tunisien : 8 chiffres, peut commencer par +216
        var cleanPhone = phone.Replace(" ", "").Replace("-", "");
        return PhoneRegex().IsMatch(cleanPhone);
    }

    [GeneratedRegex(@"^\d{8,14}$")]
    private static partial Regex BarcodeRegex();

    [GeneratedRegex(@"^[A-Z]{1,3}\d{4,10}[A-Z]?$", RegexOptions.IgnoreCase)]
    private static partial Regex AmmRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(\+216)?[2579]\d{7}$")]
    private static partial Regex PhoneRegex();
}
