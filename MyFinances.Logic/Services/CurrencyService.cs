using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class CurrencyService(
    ApplicationDbContext context,
    ILogger<CurrencyService> logger) : ICurrencyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<CurrencyService> _logger = logger;

    public async Task<CurrencyListResponseViewModel> GetCurrenciesAsync(CurrencyFilterViewModel filter)
    {
        try
        {
            var query = _context.Currencies.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Code))
            {
                query = query.Where(c => c.Code.Contains(filter.Code.ToUpper()));
            }

            if (!string.IsNullOrEmpty(filter.Name))
            {
                query = query.Where(c => c.Name.Contains(filter.Name));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filter.IsActive.Value);
            }

            // Get total count
            var total = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "name" => filter.SortDirection.ToLower() == "desc" 
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                "symbol" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.Symbol)
                    : query.OrderBy(c => c.Symbol),
                "isdefault" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.IsDefault)
                    : query.OrderBy(c => c.IsDefault),
                _ => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.Code)
                    : query.OrderBy(c => c.Code)
            };

            // Apply pagination
            var currencies = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CurrencyViewModel
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    IsDefault = c.IsDefault,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / filter.PageSize);

            return new CurrencyListResponseViewModel
            {
                Success = true,
                Data = currencies,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currencies");
            return new CurrencyListResponseViewModel
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<CurrencyViewModel?> GetCurrencyByIdAsync(int id)
    {
        try
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Id == id);

            if (currency == null)
                return null;

            return new CurrencyViewModel
            {
                Id = currency.Id,
                Code = currency.Code,
                Name = currency.Name,
                Symbol = currency.Symbol,
                IsDefault = currency.IsDefault,
                IsActive = currency.IsActive,
                CreatedAt = currency.CreatedAt,
                UpdatedAt = currency.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currency by id {Id}", id);
            return null;
        }
    }

    public async Task<CurrencyViewModel?> GetCurrencyByCodeAsync(string code)
    {
        try
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

            if (currency == null)
                return null;

            return new CurrencyViewModel
            {
                Id = currency.Id,
                Code = currency.Code,
                Name = currency.Name,
                Symbol = currency.Symbol,
                IsDefault = currency.IsDefault,
                IsActive = currency.IsActive,
                CreatedAt = currency.CreatedAt,
                UpdatedAt = currency.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currency by code {Code}", code);
            return null;
        }
    }

    public async Task<CurrencyViewModel?> GetDefaultCurrencyAsync()
    {
        try
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive);

            if (currency == null)
                return null;

            return new CurrencyViewModel
            {
                Id = currency.Id,
                Code = currency.Code,
                Name = currency.Name,
                Symbol = currency.Symbol,
                IsDefault = currency.IsDefault,
                IsActive = currency.IsActive,
                CreatedAt = currency.CreatedAt,
                UpdatedAt = currency.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default currency");
            return null;
        }
    }

    public async Task<List<CurrencyViewModel>> GetActiveCurrenciesAsync()
    {
        try
        {
            var currencies = await _context.Currencies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .Select(c => new CurrencyViewModel
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    IsDefault = c.IsDefault,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active currencies");
            return new List<CurrencyViewModel>();
        }
    }

    public async Task<CurrencyViewModel?> CreateCurrencyAsync(CurrencyCreateEditViewModel model)
    {
        try
        {
            // Check if currency code already exists
            var existingCurrency = await _context.Currencies
                .AnyAsync(c => c.Code == model.Code.ToUpper());

            if (existingCurrency)
            {
                _logger.LogWarning("Currency with code {Code} already exists", model.Code);
                return null;
            }

            // If setting as default, remove default from other currencies
            if (model.IsDefault)
            {
                await RemoveDefaultFromAllCurrenciesAsync();
            }

            var currency = new Currency
            {
                Code = model.Code.ToUpper(),
                Name = model.Name,
                Symbol = model.Symbol,
                IsDefault = model.IsDefault,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Currencies.Add(currency);
            await _context.SaveChangesAsync();

            return new CurrencyViewModel
            {
                Id = currency.Id,
                Code = currency.Code,
                Name = currency.Name,
                Symbol = currency.Symbol,
                IsDefault = currency.IsDefault,
                IsActive = currency.IsActive,
                CreatedAt = currency.CreatedAt,
                UpdatedAt = currency.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating currency");
            return null;
        }
    }

    public async Task<bool> UpdateCurrencyAsync(CurrencyCreateEditViewModel model)
    {
        try
        {
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (currency == null)
                return false;

            // Check if code already exists for another currency
            var existingCurrency = await _context.Currencies
                .AnyAsync(c => c.Code == model.Code.ToUpper() && c.Id != model.Id);

            if (existingCurrency)
            {
                _logger.LogWarning("Currency with code {Code} already exists", model.Code);
                return false;
            }

            // If setting as default, remove default from other currencies
            if (model.IsDefault && !currency.IsDefault)
            {
                await RemoveDefaultFromAllCurrenciesAsync();
            }

            currency.Code = model.Code.ToUpper();
            currency.Name = model.Name;
            currency.Symbol = model.Symbol;
            currency.IsDefault = model.IsDefault;
            currency.IsActive = model.IsActive;
            currency.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency");
            return false;
        }
    }

    public async Task<bool> DeleteCurrencyAsync(int id)
    {
        try
        {
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Id == id);
            if (currency == null)
                return false;

            // Check if currency is being used in market data
            var isInUse = await _context.MarketData.AnyAsync(md => md.CurrencyId == id);
            if (isInUse)
            {
                _logger.LogWarning("Cannot delete currency {Code} as it is being used in market data", currency.Code);
                return false;
            }

            // Don't allow deletion of default currency
            if (currency.IsDefault)
            {
                _logger.LogWarning("Cannot delete default currency {Code}", currency.Code);
                return false;
            }

            _context.Currencies.Remove(currency);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting currency");
            return false;
        }
    }

    public async Task<bool> SetDefaultCurrencyAsync(int id)
    {
        try
        {
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (currency == null)
                return false;

            // Remove default from all currencies
            await RemoveDefaultFromAllCurrenciesAsync();

            // Set new default
            currency.IsDefault = true;
            currency.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default currency");
            return false;
        }
    }

    private async Task RemoveDefaultFromAllCurrenciesAsync()
    {
        var defaultCurrencies = await _context.Currencies
            .Where(c => c.IsDefault)
            .ToListAsync();

        foreach (var currency in defaultCurrencies)
        {
            currency.IsDefault = false;
            currency.UpdatedAt = DateTime.UtcNow;
        }

        if (defaultCurrencies.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}