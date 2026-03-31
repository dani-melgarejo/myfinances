using Microsoft.EntityFrameworkCore;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _context;

    public AssetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAssetAsync(string ticker, int? currencyId = null)
    {
        var asset = new Asset
        {
            Ticker = ticker.ToUpper(),
            CurrencyId = currencyId
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAssetAsync(int id, string ticker, int? currencyId = null)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return false;

        asset.Ticker = ticker.ToUpper();
        asset.CurrencyId = currencyId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAssetAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return false;

        // Check if asset is being used in movements or market data
        var hasMovements = await _context.Movements.AnyAsync(m => m.AssetId == id);
        var hasMarketData = await _context.MarketData.AnyAsync(md => md.AssetId == id);
        var hasPossessions = await _context.Possessions.AnyAsync(p => p.AssetId == id);

        if (hasMovements || hasMarketData || hasPossessions)
            return false; // Cannot delete asset that's being used

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        return await _context.Assets
            .Include(a => a.Currency)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<PagedAssetsResult<Asset>> GetAssetsPagedAsync(int page, int perPage)
    {
        var offset = (page - 1) * perPage;

        var assets = await _context.Assets
            .Include(a => a.Currency)
            .OrderBy(a => a.Id)
            .Skip(offset)
            .Take(perPage)
            .ToListAsync();

        var total = await _context.Assets.CountAsync();

        return new PagedAssetsResult<Asset>(assets)
        {
            Items = assets,
            Total = total
        };
    }

    public async Task<IEnumerable<Asset>> SearchAssetsAsync(string query, int limit)
    {
        return await _context.Assets
            .Include(a => a.Currency)
            .Where(a => a.Ticker.Contains(query))
            .OrderBy(a => a.Ticker)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        return await _context.Assets
            .Include(a => a.Currency)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAssetsByIdsAsync(IEnumerable<int> assetIds)
    {
        var assetIdList = assetIds.ToList();
        if (!assetIdList.Any())
            return Enumerable.Empty<Asset>();

        return await _context.Assets
            .Include(a => a.Currency)
            .ToListAsync()
            .ContinueWith(t => t.Result.Where(a => assetIdList.Contains(a.Id)).ToList());
    }


}
