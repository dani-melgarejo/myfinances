using MyFinances.Logic.Models;

namespace MyFinances.Logic.Interfaces;

public interface ICurrencyService
{
    Task<CurrencyListResponseViewModel> GetCurrenciesAsync(CurrencyFilterViewModel filter);
    Task<CurrencyViewModel?> GetCurrencyByIdAsync(int id);
    Task<CurrencyViewModel?> GetCurrencyByCodeAsync(string code);
    Task<CurrencyViewModel?> GetDefaultCurrencyAsync();
    Task<List<CurrencyViewModel>> GetActiveCurrenciesAsync();
    Task<CurrencyViewModel?> CreateCurrencyAsync(CurrencyCreateEditViewModel model);
    Task<bool> UpdateCurrencyAsync(CurrencyCreateEditViewModel model);
    Task<bool> DeleteCurrencyAsync(int id);
    Task<bool> SetDefaultCurrencyAsync(int id);
}