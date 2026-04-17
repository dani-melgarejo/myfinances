using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;
using Microsoft.AspNetCore.Identity;
using MyFinances.Domain.Model;

namespace MyFinancesApp.Controllers;

public class PortfolioController(
    IPortfolioReportService portfolioReportService,
    ICurrencyService currencyService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly IPortfolioReportService _portfolioReportService = portfolioReportService;
    private readonly ICurrencyService _currencyService = currencyService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet]
    public IActionResult Report()
    {
        var model = new PortfolioReportRequestViewModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult ChartReport()
    {
        var model = new PortfolioReportRequestViewModel();
        return View(model);
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

    [HttpPost]
    public async Task<IActionResult> GetReportData([FromBody] PortfolioReportRequestViewModel request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var data = await _portfolioReportService.GetPortfolioReportAsync(
                request.FechaInicio, 
                request.FechaFin,
                user.Id,
                request.TargetCurrencyId);

            var targetCurrency = data.FirstOrDefault();
            var currencyInfo = new
            {
                code = targetCurrency?.CurrencyCode ?? "USD",
                symbol = targetCurrency?.CurrencySymbol ?? "$"
            };

            return Json(new { success = true, data, currency = currencyInfo });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> MonthlyReport()
    {
        var model = new PortfolioReportRequestViewModel
        {
            FechaInicio = DateTime.Now.AddMonths(-12),
            FechaFin = DateTime.Now
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetMonthlyReportData([FromBody] PortfolioReportRequestViewModel request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var data = await _portfolioReportService.GetMonthlyPortfolioReportAsync(
                request.FechaInicio, 
                request.FechaFin,
                user.Id,
                request.TargetCurrencyId);

            var targetCurrency = data.FirstOrDefault();
            var currencyInfo = new
            {
                code = targetCurrency?.CurrencyCode ?? "USD",
                symbol = targetCurrency?.CurrencySymbol ?? "$"
            };

            return Json(new { success = true, data, currency = currencyInfo });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Error al generar el reporte: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetDailyReportData([FromBody] PortfolioReportRequestViewModel request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var data = await _portfolioReportService.GetDailyPortfolioReportAsync(
                request.FechaInicio, 
                request.FechaFin,
                user.Id,
                request.TargetCurrencyId);

            var targetCurrency = data.FirstOrDefault();
            var currencyInfo = new
            {
                code = targetCurrency?.CurrencyCode ?? "USD",
                symbol = targetCurrency?.CurrencySymbol ?? "$"
            };

            return Json(new { success = true, data, currency = currencyInfo });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Error al generar el reporte diario: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetChartComparisonData([FromBody] PortfolioReportRequestViewModel request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var data = await _portfolioReportService.GetPortfolioChartComparisonAsync(
                request.FechaInicio, 
                request.FechaFin,
                user.Id,
                request.TargetCurrencyId);

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Error al generar el gráfico de comparación: " + ex.Message });
        }
    }
}
