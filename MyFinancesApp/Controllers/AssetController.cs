using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinancesApp.Models;

namespace MyFinancesApp.Controllers;

public class AssetController(IAssetService assetService, IHistoricService historicService) : Controller
{
    private readonly IAssetService _assetService = assetService;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAssetViewModel model)
    {
        if (ModelState.IsValid)
        {
            await _assetService.AddAssetAsync(model.Ticker);
            return RedirectToAction("Index", "Home");
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Grid(int page = 1, int perPage = 10)
    {
        try
        {
            var result = await _assetService.GetAssetsPagedAsync(page, perPage);

            var response = new
            {
                data = result.Items.Select(a => new { id = a.Id, ticker = a.Ticker }),
                total = result.Total
            };

            return Json(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q = "")
    {
        try
        {
            var assets = await _assetService.SearchAssetsAsync(q, 10);
            var data = assets.Select(a => new { id = a.Id, text = a.Ticker });

            return Json(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
