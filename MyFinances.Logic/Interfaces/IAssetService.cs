using MyFinances.Domain;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IAssetService
{
    Task AddAssetAsync(string ticker, int? currencyId = null);
    Task<bool> UpdateAssetAsync(int id, string ticker, int? currencyId = null);
    Task<bool> DeleteAssetAsync(int id);
    Task<Asset?> GetAssetByIdAsync(int id);
    Task<PagedAssetsResult<Asset>> GetAssetsPagedAsync(int page, int perPage);
    Task<IEnumerable<Asset>> SearchAssetsAsync(string query, int limit);
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
    Task<IEnumerable<Asset>> GetAssetsByIdsAsync(IEnumerable<int> assetIds);
}
