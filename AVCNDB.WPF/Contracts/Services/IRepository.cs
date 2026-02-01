using System.Linq.Expressions;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface générique du repository
/// Fournit les opérations CRUD de base
/// </summary>
/// <typeparam name="T">Type d'entité</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Récupère toutes les entités
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Récupère une entité par son ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);
    
    /// <summary>
    /// Recherche des entités selon un prédicat
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Récupère une entité unique selon un prédicat
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Ajoute une nouvelle entité
    /// </summary>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// Ajoute plusieurs entités
    /// </summary>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    
    /// <summary>
    /// Met à jour une entité existante
    /// </summary>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// Supprime une entité
    /// </summary>
    Task DeleteAsync(T entity);
    
    /// <summary>
    /// Supprime une entité par son ID
    /// </summary>
    Task DeleteByIdAsync(int id);
    
    /// <summary>
    /// Compte le nombre total d'entités
    /// </summary>
    Task<int> CountAsync();
    
    /// <summary>
    /// Compte les entités selon un prédicat
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Vérifie si une entité existe selon un prédicat
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Récupère des entités avec pagination
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false);
}

/// <summary>
/// Résultat paginé
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
