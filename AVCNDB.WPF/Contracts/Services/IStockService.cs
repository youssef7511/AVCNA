namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de gestion du stock
/// Gère les alertes et le suivi des stocks
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Récupère tous les médicaments en alerte de stock (quantité < seuil minimum)
    /// </summary>
    Task<IEnumerable<StockAlertItem>> GetLowStockAlertsAsync();
    
    /// <summary>
    /// Récupère tous les médicaments proches de la date d'expiration
    /// </summary>
    /// <param name="daysBeforeExpiry">Nombre de jours avant expiration</param>
    Task<IEnumerable<ExpiryAlertItem>> GetExpiryAlertsAsync(int daysBeforeExpiry = 90);
    
    /// <summary>
    /// Met à jour le stock d'un médicament
    /// </summary>
    /// <param name="medicId">ID du médicament</param>
    /// <param name="quantity">Nouvelle quantité</param>
    Task UpdateStockAsync(int medicId, int quantity);
    
    /// <summary>
    /// Ajoute du stock pour un médicament
    /// </summary>
    /// <param name="medicId">ID du médicament</param>
    /// <param name="quantityToAdd">Quantité à ajouter</param>
    /// <param name="batchNo">Numéro de lot</param>
    /// <param name="expiryDate">Date d'expiration</param>
    Task AddStockAsync(int medicId, int quantityToAdd, string batchNo, DateTime expiryDate);
    
    /// <summary>
    /// Retire du stock pour un médicament
    /// </summary>
    /// <param name="medicId">ID du médicament</param>
    /// <param name="quantityToRemove">Quantité à retirer</param>
    Task<bool> RemoveStockAsync(int medicId, int quantityToRemove);
    
    /// <summary>
    /// Définit les seuils d'alerte pour un médicament
    /// </summary>
    Task SetAlertThresholdsAsync(int medicId, int minStock, int maxStock);
    
    /// <summary>
    /// Nombre total d'alertes actives
    /// </summary>
    Task<int> GetTotalAlertsCountAsync();
}

/// <summary>
/// Élément d'alerte de stock bas
/// </summary>
public class StockAlertItem
{
    public int MedicId { get; set; }
    public string MedicName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public int Deficit => MinStock - CurrentStock;
    public string Severity => CurrentStock == 0 ? "Critical" : CurrentStock < MinStock / 2 ? "High" : "Medium";
}

/// <summary>
/// Élément d'alerte d'expiration
/// </summary>
public class ExpiryAlertItem
{
    public int MedicId { get; set; }
    public string MedicName { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry => (ExpiryDate - DateTime.Now).Days;
    public string Severity => DaysUntilExpiry <= 0 ? "Expired" : DaysUntilExpiry <= 30 ? "Critical" : DaysUntilExpiry <= 60 ? "High" : "Medium";
}
