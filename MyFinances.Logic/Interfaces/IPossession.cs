using MyFinances.Domain;

namespace MyFinances.Logic.Interfaces;

public interface IPossessionService
{
    Task UpdatePossessionsForAssetAsync(int assetId, string userId);
    Task<IEnumerable<Possession>> GetPossessionsByAssetAsync(int assetId, string userId);
    Task UpdateAllPossessionsCurrencyAsync();
}
