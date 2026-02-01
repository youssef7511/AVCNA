using Microsoft.EntityFrameworkCore;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de gestion du stock
/// </summary>
public class StockService : IStockService
{
    private readonly AppDbContext _context;

    public StockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StockAlertItem>> GetLowStockAlertsAsync()
    {
        var lowStockItems = await _context.Stocks
            .Where(s => s.quantity < s.minstock)
            .OrderBy(s => s.quantity)
            .Select(s => new StockAlertItem
            {
                MedicId = s.medicid,
                MedicName = s.medicname,
                CurrentStock = s.quantity,
                MinStock = s.minstock
            })
            .ToListAsync();

        return lowStockItems;
    }

    public async Task<IEnumerable<ExpiryAlertItem>> GetExpiryAlertsAsync(int daysBeforeExpiry = 90)
    {
        var expiryDate = DateTime.Now.AddDays(daysBeforeExpiry);
        
        var expiringItems = await _context.Stocks
            .Where(s => s.expirydate != null && s.expirydate <= expiryDate)
            .OrderBy(s => s.expirydate)
            .Select(s => new ExpiryAlertItem
            {
                MedicId = s.medicid,
                MedicName = s.medicname,
                BatchNo = s.batchno,
                Quantity = s.quantity,
                ExpiryDate = s.expirydate ?? DateTime.Now
            })
            .ToListAsync();

        return expiringItems;
    }

    public async Task UpdateStockAsync(int medicId, int quantity)
    {
        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.medicid == medicId);
        if (stock != null)
        {
            stock.quantity = quantity;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddStockAsync(int medicId, int quantityToAdd, string batchNo, DateTime expiryDate)
    {
        var existingStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.medicid == medicId && s.batchno == batchNo);

        if (existingStock != null)
        {
            existingStock.quantity += quantityToAdd;
        }
        else
        {
            var medic = await _context.Medics.FindAsync(medicId);
            var newStock = new Stock
            {
                medicid = medicId,
                medicname = medic?.itemname ?? "",
                quantity = quantityToAdd,
                batchno = batchNo,
                expirydate = expiryDate,
                minstock = 10, // Valeur par défaut
                maxstock = 100
            };
            await _context.Stocks.AddAsync(newStock);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveStockAsync(int medicId, int quantityToRemove)
    {
        var stock = await _context.Stocks
            .Where(s => s.medicid == medicId && s.quantity > 0)
            .OrderBy(s => s.expirydate) // FIFO par date d'expiration
            .FirstOrDefaultAsync();

        if (stock == null || stock.quantity < quantityToRemove)
        {
            return false;
        }

        stock.quantity -= quantityToRemove;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SetAlertThresholdsAsync(int medicId, int minStock, int maxStock)
    {
        var stocks = await _context.Stocks
            .Where(s => s.medicid == medicId)
            .ToListAsync();

        foreach (var stock in stocks)
        {
            stock.minstock = minStock;
            stock.maxstock = maxStock;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetTotalAlertsCountAsync()
    {
        var lowStockCount = await _context.Stocks
            .CountAsync(s => s.quantity < s.minstock);

        var expiringCount = await _context.Stocks
            .CountAsync(s => s.expirydate != null && s.expirydate <= DateTime.Now.AddDays(90));

        return lowStockCount + expiringCount;
    }
}
