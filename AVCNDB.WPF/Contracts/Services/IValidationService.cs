using System.ComponentModel.DataAnnotations;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de validation
/// Valide les entités avant sauvegarde
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Valide une entité et retourne les résultats
    /// </summary>
    /// <typeparam name="T">Type de l'entité</typeparam>
    /// <param name="entity">Entité à valider</param>
    /// <returns>Résultat de validation</returns>
    ValidationResult<T> Validate<T>(T entity) where T : class;
    
    /// <summary>
    /// Valide une propriété spécifique
    /// </summary>
    /// <param name="value">Valeur à valider</param>
    /// <param name="propertyName">Nom de la propriété</param>
    /// <param name="validationAttributes">Attributs de validation</param>
    IEnumerable<string> ValidateProperty(object? value, string propertyName, IEnumerable<ValidationAttribute> validationAttributes);
    
    /// <summary>
    /// Valide un code-barres
    /// </summary>
    bool IsValidBarcode(string barcode);
    
    /// <summary>
    /// Valide un code AMM
    /// </summary>
    bool IsValidAmm(string amm);
    
    /// <summary>
    /// Valide une adresse email
    /// </summary>
    bool IsValidEmail(string email);
    
    /// <summary>
    /// Valide un numéro de téléphone
    /// </summary>
    bool IsValidPhone(string phone);
}

/// <summary>
/// Résultat de validation générique
/// </summary>
public class ValidationResult<T>
{
    public bool IsValid { get; set; }
    public T? Entity { get; set; }
    public Dictionary<string, List<string>> Errors { get; set; } = new();
    
    public void AddError(string propertyName, string errorMessage)
    {
        if (!Errors.ContainsKey(propertyName))
        {
            Errors[propertyName] = new List<string>();
        }
        Errors[propertyName].Add(errorMessage);
        IsValid = false;
    }
    
    public IEnumerable<string> GetAllErrors()
    {
        return Errors.SelectMany(e => e.Value);
    }
}
