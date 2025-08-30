using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class MarketDataService(ApplicationDbContext context, ILogger<MarketDataService> logger) : IMarketDataService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<MarketDataService> _logger = logger;
    public async Task<MarketData> GetLastMarketDataAsync(int assetId)
    {
        return await _context.MarketData
            .Where(md => md.AssetId == assetId)
            .OrderByDescending(md => md.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<MarketDataPagedResult> GetMarketDataPagedAsync(MarketDataFilterViewModel filter)
    {
        try
        {
            var query = _context.MarketData
                .Include(md => md.Asset)
                .AsQueryable();

            // Aplicar filtros
            if (filter.AssetId.HasValue)
            {
                query = query.Where(md => md.AssetId == filter.AssetId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Ticker))
            {
                query = query.Where(md => md.Asset.Ticker.Contains(filter.Ticker));
            }

            if (filter.FechaDesde.HasValue)
            {
                query = query.Where(md => md.Date >= filter.FechaDesde.Value.Date);
            }

            if (filter.FechaHasta.HasValue)
            {
                query = query.Where(md => md.Date <= filter.FechaHasta.Value.Date);
            }

            // Aplicar ordenamiento
            query = filter.SortBy?.ToLower() switch
            {
                "ticker" => filter.SortDirection == "asc"
                    ? query.OrderBy(md => md.Asset.Ticker)
                    : query.OrderByDescending(md => md.Asset.Ticker),
                "closeprice" => filter.SortDirection == "asc"
                    ? query.OrderBy(md => md.Close)
                    : query.OrderByDescending(md => md.Close),
                _ => filter.SortDirection == "asc"
                    ? query.OrderBy(md => md.Date)
                    : query.OrderByDescending(md => md.Date)
            };

            // Contar total
            var total = await query.CountAsync();

            // Aplicar paginación
            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(md => new MarketDataViewModel
                {
                    Id = md.Id,
                    AssetId = md.AssetId,
                    Ticker = md.Asset.Ticker,
                    Date = md.Date,
                    Close = md.Close
                })
                .ToListAsync();

            return new MarketDataPagedResult
            {
                Data = data,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)total / filter.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo datos de mercado paginados");
            throw;
        }
    }

    public async Task<IEnumerable<MarketDataViewModel>> GetMarketDataByAssetAsync(int assetId, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        var query = _context.MarketData
            .Include(md => md.Asset)
            .Where(md => md.AssetId == assetId);

        if (fechaDesde.HasValue)
            query = query.Where(md => md.Date >= fechaDesde.Value.Date);

        if (fechaHasta.HasValue)
            query = query.Where(md => md.Date <= fechaHasta.Value.Date);

        return await query
            .OrderByDescending(md => md.Date)
            .Select(md => new MarketDataViewModel
            {
                Id = md.Id,
                AssetId = md.AssetId,
                Ticker = md.Asset.Ticker,
                Date = md.Date,
                Close = md.Close
            })
            .ToListAsync();
    }

    public async Task<MarketDataViewModel> GetMarketDataByIdAsync(int id)
    {
        try
        {
            var marketData = await _context.MarketData
                .Include(md => md.Asset)
                .FirstOrDefaultAsync(md => md.Id == id);

            if (marketData == null)
                return null;

            return new MarketDataViewModel
            {
                Id = marketData.Id,
                AssetId = marketData.AssetId,
                Ticker = marketData.Asset.Ticker,
                Date = marketData.Date,
                Close = marketData.Close
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo MarketData con ID {id}");
            throw;
        }
    }

    public async Task<bool> UpdateMarketDataAsync(EditMarketDataViewModel model)
    {
        try
        {
            var marketData = await _context.MarketData.FindAsync(model.Id);

            if (marketData == null)
                return false;

            // Actualizar campos
            marketData.Close = model.Close;

          
            await _context.SaveChangesAsync();

            _logger.LogInformation($"MarketData actualizado: ID {model.Id}, Asset {model.Ticker}, Fecha {model.Date:yyyy-MM-dd}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando MarketData ID {model.Id}");
            throw;
        }
    }

    public async Task<MarketDataViewModel> CreateMarketDataAsync(EditMarketDataViewModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Verificar que no exista ya un registro para esa fecha y asset
            var existingData = await _context.MarketData
                .AnyAsync(md => md.AssetId == model.AssetId && md.Date.Date == model.Date.Date);

            if (existingData)
            {
                throw new ArgumentException($"Ya existe un registro para el asset {model.Ticker} en la fecha {model.Date:yyyy-MM-dd}");
            }

            var marketData = new MarketData
            {
                AssetId = model.AssetId,
                Date = model.Date.Date,
                Close = model.Close
            };


            _context.MarketData.Add(marketData);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"MarketData creado: Asset {model.Ticker}, Fecha {model.Date:yyyy-MM-dd}");

            return await GetMarketDataByIdAsync(marketData.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, $"Error creando MarketData para asset {model.AssetId}");
            throw;
        }
    }

    public async Task<bool> DeleteMarketDataAsync(int id)
    {
        try
        {
            var marketData = await _context.MarketData.FindAsync(id);

            if (marketData == null)
                return false;

            _context.MarketData.Remove(marketData);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"MarketData eliminado: ID {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error eliminando MarketData ID {id}");
            throw;
        }
    }    
}
