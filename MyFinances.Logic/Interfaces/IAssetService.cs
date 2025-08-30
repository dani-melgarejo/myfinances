using MyFinances.Domain;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IAssetService
{
    Task AddAssetAsync(string ticker);
    Task<PagedAssetsResult<Asset>> GetAssetsPagedAsync(int page, int perPage);
    Task<IEnumerable<Asset>> SearchAssetsAsync(string query, int limit);
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
}
