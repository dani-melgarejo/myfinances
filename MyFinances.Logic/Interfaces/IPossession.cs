using MyFinances.Domain;

namespace MyFinances.Logic.Interfaces;

public interface IPossessionService
{
    Task UpdatePossessionsForAssetAsync(int assetId);
    Task<IEnumerable<Possession>> GetPossessionsByAssetAsync(int assetId);
}
