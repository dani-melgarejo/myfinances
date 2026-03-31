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

            // 1. Eliminar posesiones existentes para este asset y usuario
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM possessions WHERE asset_id = {0} AND UserId = {1}", assetId, userId);

            // 2. Insertar nuevas posesiones basadas en MarketData y Movements
            // Updated SQL to include currency_id from the asset
            var sql = @"
                    INSERT INTO possessions (date, asset_id, quantity, TotalPrice, Worth, currency_id, UserId)
                    SELECT 
                        md.Date,
                        md.asset_id,
                        COALESCE(
                            (SELECT 
                                SUM(CASE 
                                    WHEN m.Operation = 0 THEN m.Quantity  
                                    WHEN m.Operation = 1 THEN -m.Quantity 
                                    ELSE 0 
                                END)
                             FROM movements m 
                             WHERE m.asset_id = md.asset_id 
                               AND m.Date <= md.Date
                               AND m.UserId = {2}
                            ), 0
                        ) as Quantity,
                        COALESCE(
                            (SELECT 
                                SUM(CASE 
                                    WHEN m.Operation = 0 THEN m.Quantity  
                                    WHEN m.Operation = 1 THEN -m.Quantity 
                                    ELSE 0 
                                END)
                             FROM movements m 
                             WHERE m.asset_id = md.asset_id 
                               AND m.Date <= md.Date
                               AND m.UserId = {2}
                            ), 0
                        ) * md.Close as TotalPrice,
                        0 as Worth, -- Se calculará después
                        {1} as currency_id, -- Currency from asset
                        {2} as UserId -- User from parameter
                    FROM stocks_data md
                    WHERE md.asset_id = {0}
                    ORDER BY md.Date";

            await _context.Database.ExecuteSqlRawAsync(sql, assetId, asset.CurrencyId, userId);

            // 3. Calcular Worth usando CTE equivalente
            await CalculateWorthForAssetAsync(assetId, userId);

            _logger.LogInformation($"✅ Posesiones actualizadas para asset {assetId} con moneda {asset.Currency?.Code ?? "USD"}");
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

            // Update possessions to match their asset's currency
            var sql = @"
                UPDATE possessions p
                INNER JOIN assets a ON p.asset_id = a.Id
                SET p.currency_id = a.currency_id
                WHERE p.currency_id != a.currency_id 
                   OR (p.currency_id IS NULL AND a.currency_id IS NOT NULL)
                   OR (p.currency_id IS NOT NULL AND a.currency_id IS NULL)";

            var updatedRows = await _context.Database.ExecuteSqlRawAsync(sql);
            
            _logger.LogInformation($"✅ Updated currency for {updatedRows} possessions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency information for possessions");
            throw;
        }
    }
}