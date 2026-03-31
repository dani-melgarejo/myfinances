using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinancesApp.Controllers;

public class StockQuoteController(IStockQuoteService stockQuoteService) : Controller
{
    private readonly IStockQuoteService _stockQuoteService = stockQuoteService;

    [HttpGet]
    public IActionResult Index()
    {
        var model = new StockQuoteRequestViewModel
        {
            FechaDesde = DateTime.Today.AddMonths(-1),
            FechaHasta = DateTime.Today
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetQuotes([FromBody] StockQuoteRequestViewModel request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Ticker))
            {
                return Json(new { success = false, error = "El ticker es requerido" });
            }

            var result = await _stockQuoteService.GetStockQuotesAsync(
                request.Ticker, 
                request.FechaDesde, 
                request.FechaHasta);

            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveQuotes([FromBody] SaveQuotesRequestViewModel request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Ticker) || request.Quotes == null || !request.Quotes.Any())
            {
                return Json(new { success = false, error = "Datos incompletos para guardar" });
            }

            var success = await _stockQuoteService.SaveQuotesToDatabaseAsync(request.Ticker, request.Quotes);

            if (success)
            {
                return Json(new { success = true, message = "Cotizaciones guardadas correctamente en la base de datos" });
            }
            else
            {
                return Json(new { success = false, error = "Error al guardar las cotizaciones" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class SaveQuotesRequestViewModel
{
    public string Ticker { get; set; } = string.Empty;
    public List<StockQuoteResultViewModel> Quotes { get; set; } = new();
}