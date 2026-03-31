using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface ICurrencyExchangeRateService
{
    // CRUD Operations
    Task<CurrencyExchangeRateListResponseViewModel> GetExchangeRatesAsync(CurrencyExchangeRateFilterViewModel filter);
    Task<CurrencyExchangeRateViewModel?> GetExchangeRateByIdAsync(int id);
    Task<CurrencyExchangeRateViewModel?> CreateExchangeRateAsync(CurrencyExchangeRateCreateEditViewModel model);
    Task<bool> UpdateExchangeRateAsync(CurrencyExchangeRateCreateEditViewModel model);
    Task<bool> DeleteExchangeRateAsync(int id);

    // Latest Exchange Rates
    Task<LatestExchangeRateViewModel?> GetLatestExchangeRateAsync(int fromCurrencyId, int toCurrencyId);
    Task<LatestExchangeRateViewModel?> GetLatestExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode);
    Task<List<LatestExchangeRateViewModel>> GetAllLatestExchangeRatesAsync();
    Task<List<LatestExchangeRateViewModel>> GetLatestExchangeRatesForCurrencyAsync(int currencyId);
    
    // Update Latest Rates
    Task<bool> SetLatestExchangeRateAsync(int fromCurrencyId, int toCurrencyId, decimal rate, string? source = null);
    Task<bool> UpdateLatestExchangeRatesAsync(List<CurrencyExchangeRateCreateEditViewModel> rates);

    // Currency Conversion
    Task<CurrencyConversionResponseViewModel> ConvertCurrencyAsync(CurrencyConversionRequestViewModel request);
    Task<decimal?> GetConversionRateAsync(int fromCurrencyId, int toCurrencyId, DateTime? date = null);

    // Bulk Operations
    Task<bool> ImportExchangeRatesAsync(List<CurrencyExchangeRateCreateEditViewModel> rates);
    Task<int> CleanupOldExchangeRatesAsync(DateTime cutoffDate);
}