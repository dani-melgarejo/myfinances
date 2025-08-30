using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;

namespace MyFinancesApp.Controllers;

public class HistoricController(
    IAssetService assetService,
    IHistoricService historicService,
    IMarketDataService marketDataService,
    IPossessionService possessionService) : Controller
{
    private readonly IAssetService _assetService = assetService;
    private readonly IHistoricService _historicService = historicService;
    private readonly IMarketDataService _marketDataService = marketDataService;
    private readonly IPossessionService possessionService = possessionService;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CompleteAllAssets()
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync();
            var today = DateTime.Now.Date;

            foreach (var asset in assets)
            {
                var lastData = await _marketDataService.GetLastMarketDataAsync(asset.Id);

                if (lastData != null && lastData.Date >= today)
                {
                    continue; // Skip if data is already up to date
                }

                DateTime dateFrom = new(2020, 1, 1);
                if (lastData != null)
                {
                    dateFrom = lastData.Date.AddDays(1).Date;
                }

                await _historicService.GetPostsAsync(asset.Id, dateFrom, null);
            }

            foreach (var asset in assets)
            {
                await possessionService.UpdatePossessionsForAssetAsync(asset.Id);
            }
            return Json(new { message = "Datos históricos completados para todos los assets" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}