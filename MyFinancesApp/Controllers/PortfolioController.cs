using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinancesApp.Controllers;

public class PortfolioController(IPortfolioReportService portfolioReportService) : Controller
{
    private readonly IPortfolioReportService _portfolioReportService = portfolioReportService;

    [HttpGet]
    public IActionResult Report()
    {
        var model = new PortfolioReportRequestViewModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetReportData([FromBody] PortfolioReportRequestViewModel request)
    {
        try
        {
            var data = await _portfolioReportService.GetPortfolioReportAsync(request.FechaInicio, request.FechaFin);
            return Json(new { success = true, data = data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}
