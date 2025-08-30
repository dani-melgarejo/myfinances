using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class MovementService(ApplicationDbContext context, IPossessionService possessionService, ILogger<MovementService> logger) : IMovementService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IPossessionService possessionService = possessionService;
    private readonly ILogger<MovementService> _logger = logger;
    
    public async Task AddMovementAsync(int assetId, int operation, DateTime date, decimal quantity, decimal price, int type)
    {
        var movement = new Movement
        {
            AssetId = assetId,
            Operation = operation,
            Date = date,
            Quantity = quantity,
            Price = price,
            Type = type
        };

        _context.Movements.Add(movement);
        await _context.SaveChangesAsync();
    }

    public async Task<StockMovementPagedResult> GetMovementsPagedAsync(StockMovementFilterViewModel filter)
    {
        try
        {
            var query = _context.Movements
                .Include(m => m.Asset)
                .AsQueryable();

            // Aplicar filtros
            if (filter.AssetId.HasValue)
            {
                query = query.Where(m => m.AssetId == filter.AssetId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Ticker))
            {
                query = query.Where(m => m.Asset.Ticker.Contains(filter.Ticker));
            }

            if (filter.Operation.HasValue)
            {
                query = query.Where(m => m.Operation == filter.Operation);
            }

            if (filter.Type.HasValue)
            {
                query = query.Where(m => m.Type == filter.Type);
            }

            if (filter.FechaDesde.HasValue)
            {
                query = query.Where(m => m.Date >= filter.FechaDesde.Value.Date);
            }

            if (filter.FechaHasta.HasValue)
            {
                query = query.Where(m => m.Date <= filter.FechaHasta.Value.Date);
            }

            // Calcular totales antes de paginación
            var totalInvested = await query
                .Where(m => m.Operation == 0)
                .SumAsync(m => m.Quantity * m.Price);

            var totalSold = await query
                .Where(m => m.Operation == 1)
                .SumAsync(m => m.Quantity * m.Price);

            // Aplicar ordenamiento
            query = filter.SortBy?.ToLower() switch
            {
                "ticker" => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Asset.Ticker)
                    : query.OrderByDescending(m => m.Asset.Ticker),
                "operation" => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Operation)
                    : query.OrderByDescending(m => m.Operation),
                "quantity" => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Quantity)
                    : query.OrderByDescending(m => m.Quantity),
                "price" => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Price)
                    : query.OrderByDescending(m => m.Price),
                "type" => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Type)
                    : query.OrderByDescending(m => m.Type),
                _ => filter.SortDirection == "asc"
                    ? query.OrderBy(m => m.Date)
                    : query.OrderByDescending(m => m.Date)
            };

            // Contar total
            var total = await query.CountAsync();

            // Aplicar paginación
            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new StockMovementViewModel()
                {
                    Id = m.Id,
                    AssetId = m.AssetId,
                    Ticker = m.Asset.Ticker,
                    Operation = m.Operation,
                    Date = m.Date,
                    Quantity = m.Quantity,
                    Price = m.Price,
                    Type = m.Type
                })
                .ToListAsync();

            return new StockMovementPagedResult
            {
                Data = data,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)total / filter.PageSize),
                TotalInvested = totalInvested,
                TotalSold = totalSold,
                NetInvestment = totalInvested - totalSold
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo movimientos paginados");
            throw;
        }
    }

    public async Task<StockMovementViewModel> GetMovementByIdAsync(int id)
    {
        try
        {
            var movement = await _context.Movements
                .Include(m => m.Asset)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movement == null)
                return null;

            return new StockMovementViewModel
            {
                Id = movement.Id,
                AssetId = movement.AssetId,
                Ticker = movement.Asset.Ticker,
                Operation = movement.Operation,
                Date = movement.Date,
                Quantity = movement.Quantity,
                Price = movement.Price,
                Type = movement.Type
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo movimiento con ID {id}");
            throw;
        }
    }

    public async Task<bool> UpdateMovementAsync(StockMovementViewModel model)
    {

        try
        {
            var movement = await _context.Movements.FindAsync(model.Id);

            if (movement == null)
                return false;

            // Actualizar campos
            movement.AssetId = model.AssetId;
            movement.Operation = model.Operation;
            movement.Date = model.Date.Date;
            movement.Quantity = model.Quantity;
            movement.Price = model.Price;
            movement.Type = model.Type;

            await _context.SaveChangesAsync();

            await possessionService.UpdatePossessionsForAssetAsync(movement.AssetId);

            _logger.LogInformation($"Movimiento actualizado: ID {model.Id}, Asset {model.Ticker}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error actualizando movimiento ID {model.Id}");
            throw;
        }
    }

    public async Task<bool> DeleteMovementAsync(int id)
    {
        try
        {
            var movement = await _context.Movements.FindAsync(id);

            if (movement == null)
                return false;

            _context.Movements.Remove(movement);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Movimiento eliminado: ID {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error eliminando movimiento ID {id}");
            throw;
        }
    }
}
