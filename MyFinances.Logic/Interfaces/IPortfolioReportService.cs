using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;
public interface IPortfolioReportService
{
    Task<IEnumerable<PortfolioReportViewModel>> GetPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin);
}
