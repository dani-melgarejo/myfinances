using MyFinances.Domain;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IMarketDataService
{
    Task<MarketData> GetLastMarketDataAsync(int assetId);
    Task<MarketDataPagedResult> GetMarketDataPagedAsync(MarketDataFilterViewModel filter);
    Task<IEnumerable<MarketDataViewModel>> GetMarketDataByAssetAsync(int assetId, DateTime? fechaDesde = null, DateTime? fechaHasta = null);
    Task<MarketDataViewModel> GetMarketDataByIdAsync(int id);
    Task<bool> UpdateMarketDataAsync(EditMarketDataViewModel model);
    Task<bool> DeleteMarketDataAsync(int id);
    Task<MarketDataViewModel> CreateMarketDataAsync(EditMarketDataViewModel model);
}