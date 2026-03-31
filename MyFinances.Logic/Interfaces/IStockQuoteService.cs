using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IStockQuoteService
{
    Task<StockQuoteResponseViewModel> GetStockQuotesAsync(string ticker, DateTime fechaDesde, DateTime fechaHasta);
    Task<bool> SaveQuotesToDatabaseAsync(string ticker, List<StockQuoteResultViewModel> quotes);
}