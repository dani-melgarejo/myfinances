using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IPortfolioReportService
{
    Task<IEnumerable<PortfolioReportViewModel>> GetPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null);
    Task<IEnumerable<MonthlyPortfolioReportViewModel>> GetMonthlyPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null);
    Task<IEnumerable<DailyPortfolioReportViewModel>> GetDailyPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null);
    Task<IEnumerable<PortfolioChartComparisonViewModel>> GetPortfolioChartComparisonAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null);
}
