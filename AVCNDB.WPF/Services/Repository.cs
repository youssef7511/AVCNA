using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;
using Serilog;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Implémentation générique du repository
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        var key = entityType?.FindPrimaryKey();
        var keyProperty = key?.Properties.FirstOrDefault();
        if (keyProperty == null)
        {
            return await _dbSet.FindAsync(id);
        }

        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, keyProperty.Name);
        var convertedId = Expression.Convert(Expression.Constant(id), property.Type);
        var equals = Expression.Equal(property, convertedId);
        var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);

        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(lambda);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        await _dbSet.AddRangeAsync(entityList);
        await _context.SaveChangesAsync();
        return entityList;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            DetachConflictingTrackedInstance(entity);
            _dbSet.Attach(entity);
            entry.State = EntityState.Modified;
        }
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DetachConflictingTrackedInstance(entity);

        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.deletedat = DateTime.Now;
            _context.Entry(entity).State = EntityState.Modified;
            Log.Information("Soft-delete {EntityType} (ID: {Id})", typeof(T).Name, GetEntityId(entity));
        }
        else
        {
            _dbSet.Remove(entity);
            Log.Information("Hard-delete {EntityType} (ID: {Id})", typeof(T).Name, GetEntityId(entity));
        }
        await _context.SaveChangesAsync();
    }

    private void DetachConflictingTrackedInstance(T entity)
    {
        var key = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
        var keyProperty = key?.Properties.FirstOrDefault();
        if (keyProperty == null) return;

        var keyValue = typeof(T).GetProperty(keyProperty.Name)?.GetValue(entity);
        if (keyValue == null) return;

        var existing = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e =>
                typeof(T).GetProperty(keyProperty.Name)?.GetValue(e.Entity)?.Equals(keyValue) == true);
        if (existing != null && !ReferenceEquals(existing.Entity, entity))
        {
            existing.State = EntityState.Detached;
        }
    }

    private static object? GetEntityId(T entity)
    {
        var prop = typeof(T).GetProperty("recordid");
        return prop?.GetValue(entity);
    }

    public virtual async Task DeleteByIdAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }

    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber, 
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();

        if (orderBy != null)
        {
            query = descending 
                ? query.OrderByDescending(orderBy) 
                : query.OrderBy(orderBy);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
