using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinancesApp.Controllers;

public class MarketDataController(IMarketDataService marketDataService) : Controller
{
    private readonly IMarketDataService _marketDataService = marketDataService;

    [HttpGet]
    public IActionResult Index()
    {
        var model = new MarketDataFilterViewModel
        {
            FechaDesde = DateTime.Today.AddDays(-30),
            FechaHasta = DateTime.Today
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] MarketDataFilterViewModel filter)
    {
        try
        {
            var result = await _marketDataService.GetMarketDataPagedAsync(filter);
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByAsset(int assetId, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        try
        {
            var data = await _marketDataService.GetMarketDataByAssetAsync(assetId, fechaDesde, fechaHasta);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // Controllers/MarketDataController.cs (agregar métodos)
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var marketData = await _marketDataService.GetMarketDataByIdAsync(id);

            if (marketData == null)
            {
                TempData["Error"] = "No se encontró el registro de cotización.";
                return RedirectToAction("Index");
            }

            var model = new EditMarketDataViewModel
            {
                Id = marketData.Id,
                AssetId = marketData.AssetId,
                Ticker = marketData.Ticker,
                Date = marketData.Date,
                Close = marketData.Close
            };

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar la cotización: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditMarketDataViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var success = await _marketDataService.UpdateMarketDataAsync(model);

            if (success)
            {
                TempData["Success"] = "Cotización actualizada correctamente.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar la cotización.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Create(int? assetId = null, DateTime? date = null)
    {
        var model = new EditMarketDataViewModel();

        if (assetId.HasValue)
        {
            model.AssetId = assetId.Value;
        }

        if (date.HasValue)
        {
            model.Date = date.Value;
        }
        else
        {
            model.Date = DateTime.Today;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EditMarketDataViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _marketDataService.CreateMarketDataAsync(model);

            if (result != null)
            {
                TempData["Success"] = "Cotización creada correctamente.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "No se pudo crear la cotización.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear: {ex.Message}";
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _marketDataService.DeleteMarketDataAsync(id);

            if (success)
            {
                return Json(new { success = true, message = "Cotización eliminada correctamente." });
            }
            else
            {
                return Json(new { success = false, error = "No se encontró la cotización." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        try
        {
            var marketData = await _marketDataService.GetMarketDataByIdAsync(id);

            if (marketData == null)
            {
                return Json(new { success = false, error = "No se encontró la cotización." });
            }

            return Json(new { success = true, data = marketData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}