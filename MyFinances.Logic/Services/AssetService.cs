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

    public async Task AddAssetAsync(string ticker)
    {
        var asset = new Asset { Ticker = ticker.ToUpper() };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedAssetsResult<Asset>> GetAssetsPagedAsync(int page, int perPage)
    {
        var offset = (page - 1) * perPage;

        var assets = await _context.Assets
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
            .Where(a => a.Ticker.Contains(query))
            .OrderBy(a => a.Ticker)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        return await _context.Assets.ToListAsync();
    }
}
