using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinancesApp.Controllers;

public class StocksController(IMovementService movementService) : Controller
{
    private IMovementService _movementService => movementService;

    [HttpGet]
    public IActionResult Index()
    {
        var model = new StockMovementFilterViewModel
        {
            FechaDesde = DateTime.Today.AddDays(-30),
            FechaHasta = DateTime.Today
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] StockMovementFilterViewModel filter)
    {
        try
        {
            var result = await _movementService.GetMovementsPagedAsync(filter);
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult CreateOrEdit()
    {

        return View(new StockMovementViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrEdit(StockMovementViewModel model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                await _movementService.AddMovementAsync(
                    model.AssetId,
                    model.Operation,
                    model.Date,
                    model.Quantity,
                    model.Price,
                    model.Type
                );

                TempData["Success"] = "Operación registrada exitosamente.";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al registrar la operación: {ex.Message}";
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var movement = await _movementService.GetMovementByIdAsync(id);

            if (movement == null)
            {
                TempData["Error"] = "No se encontró la operación.";
                return RedirectToAction("Index");
            }

            return View("CreateOrEdit", movement);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar la operación: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StockMovementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var success = await _movementService.UpdateMovementAsync(model);

            if (success)
            {
                TempData["Success"] = "Operación actualizada correctamente.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar la operación.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _movementService.DeleteMovementAsync(id);

            if (success)
            {
                return Json(new { success = true, message = "Operación eliminada correctamente." });
            }
            else
            {
                return Json(new { success = false, error = "No se encontró la operación." });
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
            var movement = await _movementService.GetMovementByIdAsync(id);

            if (movement == null)
            {
                return Json(new { success = false, error = "No se encontró la operación." });
            }

            return Json(new { success = true, data = movement });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}