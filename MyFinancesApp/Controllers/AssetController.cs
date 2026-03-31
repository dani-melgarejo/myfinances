using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinancesApp.Models;

namespace MyFinancesApp.Controllers;

public class AssetController(
    IAssetService assetService, 
    IHistoricService historicService,
    ICurrencyService currencyService) : Controller
{
    private readonly IAssetService _assetService = assetService;
    private readonly ICurrencyService _currencyService = currencyService;

    [HttpGet]
    public IActionResult Index()
    {
        return View(new CreateAssetViewModel { Ticker = "" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAssetViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _assetService.AddAssetAsync(model.Ticker, model.CurrencyId);
                TempData["Success"] = "Asset creado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear el asset: {ex.Message}";
            }
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                TempData["Error"] = "Asset no encontrado.";
                return RedirectToAction("Index");
            }

            var model = new EditAssetViewModel
            {
                Id = asset.Id,
                Ticker = asset.Ticker,
                CurrencyId = asset.CurrencyId,
                CurrencyCode = asset.Currency?.Code,
                CurrencySymbol = asset.Currency?.Symbol
            };

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar el asset: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditAssetViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var success = await _assetService.UpdateAssetAsync(model.Id, model.Ticker, model.CurrencyId);
                if (success)
                {
                    TempData["Success"] = "Asset actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el asset.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar el asset: {ex.Message}";
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _assetService.DeleteAssetAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Asset eliminado correctamente." });
            }
            else
            {
                return Json(new { success = false, error = "No se puede eliminar el asset porque está siendo utilizado." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrencies()
    {
        try
        {
            var currencies = await _currencyService.GetActiveCurrenciesAsync();
            var result = currencies.Select(c => new {
                id = c.Id,
                text = $"{c.Code} - {c.Name}",
                code = c.Code,
                symbol = c.Symbol,
                isDefault = c.IsDefault
            }).ToList();
            
            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Grid(int page = 1, int perPage = 10)
    {
        try
        {
            var result = await _assetService.GetAssetsPagedAsync(page, perPage);

            var response = new
            {
                data = result.Items.Select(a => new { 
                    id = a.Id, 
                    ticker = a.Ticker,
                    currencyCode = a.Currency?.Code ?? "USD",
                    currencySymbol = a.Currency?.Symbol ?? "$"
                }),
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
            var data = assets.Select(a => new { 
                id = a.Id, 
                text = $"{a.Ticker} ({a.Currency?.Code ?? "USD"})" 
            });

            return Json(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
