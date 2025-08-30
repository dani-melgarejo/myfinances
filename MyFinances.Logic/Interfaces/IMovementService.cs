using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IMovementService
{
    Task AddMovementAsync(int assetId, int operation, DateTime date, decimal quantity, decimal price, int type);
    Task<StockMovementPagedResult> GetMovementsPagedAsync(StockMovementFilterViewModel filter);
    Task<StockMovementViewModel> GetMovementByIdAsync(int id);
    Task<bool> UpdateMovementAsync(StockMovementViewModel model);
    Task<bool> DeleteMovementAsync(int id);
}