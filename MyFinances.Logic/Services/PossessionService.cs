using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;

namespace MyFinances.Logic.Services;

public class PossessionService(ApplicationDbContext context, ILogger<PossessionService> logger) : IPossessionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<PossessionService> _logger = logger;

    public async Task UpdatePossessionsForAssetAsync(int assetId, string userId)
    {
        try
        {
            _logger.LogInformation($"Actualizando posesiones para asset {assetId}");

            // Get the asset with its currency information
            var asset = await _context.Assets
                .Include(a => a.Currency)
                .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset == null)
            {
                _logger.LogWarning($"Asset {assetId} not found");
                return;
            }

            // 1. Eliminar posesiones existentes para este asset y usuario usando EF
            var existingPossessions = await _context.Possessions
                .Where(p => p.AssetId == assetId && p.UserId == userId)
                .ToListAsync();

            if (existingPossessions.Any())
            {
                _context.Possessions.RemoveRange(existingPossessions);
                await _context.SaveChangesAsync();
            }

            // 2. Obtener todos los datos de mercado para este asset
            var marketDataList = await _context.MarketData
                .Where(md => md.AssetId == assetId)
                .OrderBy(md => md.Date)
                .ToListAsync();

            if (!marketDataList.Any())
            {
                _logger.LogWarning($"No hay datos de mercado para asset {assetId}");
                return;
            }

            // 3. Obtener todos los movimientos del usuario para este asset
            var movements = await _context.Movements
                .Where(m => m.AssetId == assetId && m.UserId == userId)
                .OrderBy(m => m.Date)
                .ToListAsync();

            // 4. Crear nuevas posesiones basadas en MarketData y Movements
            var newPossessions = new List<Possession>();

            foreach (var marketData in marketDataList)
            {
                // Calcular la cantidad acumulada hasta esta fecha
                var quantityUntilDate = movements
                    .Where(m => m.Date <= marketData.Date)
                    .Sum(m => m.Operation == 0 ? m.Quantity : -m.Quantity);

                // Calcular el precio total (cantidad * precio de cierre)
                var totalPrice = quantityUntilDate * marketData.Close;

                // Crear la posesión
                var possession = new Possession
                {
                    Date = marketData.Date,
                    AssetId = assetId,
                    Quantity = quantityUntilDate,
                    TotalPrice = totalPrice,
                    Worth = 0, // Se calculará después
                    CurrencyId = asset.CurrencyId,
                    UserId = userId,
                    Asset = asset
                };

                newPossessions.Add(possession);
            }

            // 5. Agregar todas las posesiones a la base de datos
            if (newPossessions.Any())
            {
                await _context.Possessions.AddRangeAsync(newPossessions);
                await _context.SaveChangesAsync();

                // 6. Calcular Worth
                await CalculateWorthForAssetAsync(assetId, userId);

                _logger.LogInformation($"✅ {newPossessions.Count} posesiones creadas para asset {assetId} con moneda {asset.Currency?.Code ?? "USD"}");
            }
            else
            {
                _logger.LogWarning($"No se crearon posesiones para asset {assetId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando posesiones para asset {assetId}");
            throw;
        }
    }

    private async Task CalculateWorthForAssetAsync(int assetId, string userId)
    {
        // Obtener movimientos diarios agrupados
        var dailyMovements = await _context.Movements
            .Where(m => m.AssetId == assetId && m.UserId == userId)
            .GroupBy(m => new { m.AssetId, m.Date })
            .Select(g => new
            {
                g.Key.AssetId,
                g.Key.Date,
                TotalPurchases = g.Where(m => m.Operation == 0)
                                 .Sum(m => m.Quantity * m.Price),
                TotalSales = g.Where(m => m.Operation == 1)
                             .Sum(m => m.Quantity * m.Price),
                NetQuantityChange = g.Sum(m => m.Operation == 0 ? m.Quantity : -m.Quantity)
            })
            .ToListAsync();

        // Obtener todas las posesiones ordenadas por fecha
        var possessions = await _context.Possessions
            .Where(p => p.AssetId == assetId && p.UserId == userId)
            .OrderBy(p => p.Date)
            .ToListAsync();

        // Calcular worth para cada posesión
        for (int i = 0; i < possessions.Count(); i++)
        {
            var current = possessions[i];
            var previous = i > 0 ? possessions[i - 1] : null;

            var dailyMovement = dailyMovements
                .FirstOrDefault(dm => dm.Date.Date == current.Date.Date);

            decimal calculatedWorth;

            if (previous == null)
            {
                // Primera posesión, worth = 0
                calculatedWorth = 0;
            }
            else if (dailyMovement == null)
            {
                // No hubo movimientos en el día
                calculatedWorth = current.TotalPrice - previous.TotalPrice;
            }
            else
            {
                // Hubo movimientos, ajustar por operaciones
                calculatedWorth = current.TotalPrice - previous.TotalPrice
                                - dailyMovement.TotalPurchases
                                + dailyMovement.TotalSales;
            }

            current.Worth = calculatedWorth;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Possession>> GetPossessionsByAssetAsync(int assetId, string userId)
    {
        return await _context.Possessions
            .Include(p => p.Asset)
            .Include(p => p.Currency)  // Include currency information
            .Where(p => p.AssetId == assetId && p.UserId == userId)
            .OrderBy(p => p.Date)
            .ToListAsync();
    }

    public async Task UpdateAllPossessionsCurrencyAsync()
    {
        try
        {
            _logger.LogInformation("Updating currency information for all possessions");

            // Get all possessions with their assets
            var possessions = await _context.Possessions
                .Include(p => p.Asset)
                .Where(p => p.Asset != null && 
                       (p.CurrencyId != p.Asset.CurrencyId || 
                        (p.CurrencyId == null && p.Asset.CurrencyId != null) ||
                        (p.CurrencyId != null && p.Asset.CurrencyId == null)))
                .ToListAsync();

            if (possessions.Any())
            {
                // Update each possession's currency to match its asset
                foreach (var possession in possessions)
                {
                    possession.CurrencyId = possession.Asset!.CurrencyId;
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"✅ Updated currency for {possessions.Count} possessions");
            }
            else
            {
                _logger.LogInformation("No possessions needed currency update");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency information for possessions");
            throw;
        }
    }
}