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

    public async Task UpdatePossessionsForAssetAsync(int assetId)
    {
        try
        {
            _logger.LogInformation($"Actualizando posesiones para asset {assetId}");

            // 1. Eliminar posesiones existentes para este asset
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Possessions WHERE asset_id = {0}", assetId);

            // 2. Insertar nuevas posesiones basadas en MarketData y Movements
            var sql = @"
                    INSERT INTO Possessions (date, asset_id, quantity, TotalPrice, Worth)
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
                             FROM Movements m 
                             WHERE m.asset_id = md.asset_id 
                               AND m.Date <= md.Date
                            ), 0
                        ) as Quantity,
                        COALESCE(
                            (SELECT 
                                SUM(CASE 
                                    WHEN m.Operation = 0 THEN m.Quantity  
                                    WHEN m.Operation = 1 THEN -m.Quantity 
                                    ELSE 0 
                                END)
                             FROM Movements m 
                             WHERE m.asset_id = md.asset_id 
                               AND m.Date <= md.Date
                            ), 0
                        ) * md.[Close] as TotalPrice,
                        0 as Worth -- Se calculará después
                    FROM stocks_data md
                    WHERE md.asset_id = {0}
                    ORDER BY md.Date";

            await _context.Database.ExecuteSqlRawAsync(sql, assetId);

            // 3. Calcular Worth usando CTE equivalente
            await CalculateWorthForAssetAsync(assetId);

            _logger.LogInformation($"✅ Posesiones actualizadas para asset {assetId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando posesiones para asset {assetId}");
            throw;
        }
    }

    private async Task CalculateWorthForAssetAsync(int assetId)
    {
        // Obtener movimientos diarios agrupados
        var dailyMovements = await _context.Movements
            .Where(m => m.AssetId == assetId)
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
            .Where(p => p.AssetId == assetId)
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

    public async Task<IEnumerable<Possession>> GetPossessionsByAssetAsync(int assetId)
    {
        return await _context.Possessions
            .Include(p => p.Asset)
            .Where(p => p.AssetId == assetId)
            .OrderBy(p => p.Date)
            .ToListAsync();
    }
}