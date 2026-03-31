using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface IExchangeRateApiService
{
    Task<ExchangeRateApiResponse?> GetLatestExchangeRatesAsync(string baseCurrency = "USD");
    Task<ExchangeRateUpdateResponse> UpdateAllExchangeRatesAsync(ExchangeRateUpdateRequest? request = null);
    Task<ExchangeRateUpdateResponse> UpdateSpecificExchangeRatesAsync(List<string> currencyCodes, string source = "ExchangeRate-API");
}