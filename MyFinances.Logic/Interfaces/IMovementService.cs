using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IMovementService
{
    Task AddMovementAsync(int assetId, int operation, DateTime date, decimal quantity, decimal price, int type, int? currencyId = null, string? userId = null);
    Task<StockMovementPagedResult> GetMovementsPagedAsync(StockMovementFilterViewModel filter);
    Task<StockMovementViewModel> GetMovementByIdAsync(int id);
    Task<bool> UpdateMovementAsync(StockMovementViewModel model);
    Task<bool> DeleteMovementAsync(int id);
    Task<IEnumerable<int>> GetAssetIdsByUserAsync(string userId);
}