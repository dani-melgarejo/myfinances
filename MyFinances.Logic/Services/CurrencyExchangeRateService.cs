using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class CurrencyExchangeRateService(
    ApplicationDbContext context,
    ILogger<CurrencyExchangeRateService> logger) : ICurrencyExchangeRateService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<CurrencyExchangeRateService> _logger = logger;

    public async Task<CurrencyExchangeRateListResponseViewModel> GetExchangeRatesAsync(CurrencyExchangeRateFilterViewModel filter)
    {
        try
        {
            var query = _context.CurrencyExchangeRates
                .Include(cer => cer.FromCurrency)
                .Include(cer => cer.ToCurrency)
                .AsQueryable();

            // Apply filters
            if (filter.FromCurrencyId.HasValue)
            {
                query = query.Where(cer => cer.FromCurrencyId == filter.FromCurrencyId.Value);
            }

            if (filter.ToCurrencyId.HasValue)
            {
                query = query.Where(cer => cer.ToCurrencyId == filter.ToCurrencyId.Value);
            }

            if (!string.IsNullOrEmpty(filter.FromCurrencyCode))
            {
                query = query.Where(cer => cer.FromCurrency.Code.Contains(filter.FromCurrencyCode.ToUpper()));
            }

            if (!string.IsNullOrEmpty(filter.ToCurrencyCode))
            {
                query = query.Where(cer => cer.ToCurrency.Code.Contains(filter.ToCurrencyCode.ToUpper()));
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(cer => cer.Date >= filter.DateFrom.Value.Date);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(cer => cer.Date <= filter.DateTo.Value.Date);
            }

            if (filter.IsLatest.HasValue)
            {
                query = query.Where(cer => cer.IsLatest == filter.IsLatest.Value);
            }

            if (!string.IsNullOrEmpty(filter.Source))
            {
                query = query.Where(cer => cer.Source != null && cer.Source.Contains(filter.Source));
            }

            // Get total count
            var total = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "fromcurrency" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.FromCurrency.Code)
                    : query.OrderBy(cer => cer.FromCurrency.Code),
                "tocurrency" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.ToCurrency.Code)
                    : query.OrderBy(cer => cer.ToCurrency.Code),
                "rate" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.Rate)
                    : query.OrderBy(cer => cer.Rate),
                "islatest" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.IsLatest)
                    : query.OrderBy(cer => cer.IsLatest),
                "source" => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.Source)
                    : query.OrderBy(cer => cer.Source),
                _ => filter.SortDirection.ToLower() == "desc"
                    ? query.OrderByDescending(cer => cer.Date)
                    : query.OrderBy(cer => cer.Date)
            };

            // Apply pagination
            var exchangeRates = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(cer => new CurrencyExchangeRateViewModel
                {
                    Id = cer.Id,
                    FromCurrencyId = cer.FromCurrencyId,
                    FromCurrencyCode = cer.FromCurrency.Code,
                    FromCurrencyName = cer.FromCurrency.Name,
                    FromCurrencySymbol = cer.FromCurrency.Symbol,
                    ToCurrencyId = cer.ToCurrencyId,
                    ToCurrencyCode = cer.ToCurrency.Code,
                    ToCurrencyName = cer.ToCurrency.Name,
                    ToCurrencySymbol = cer.ToCurrency.Symbol,
                    Rate = cer.Rate,
                    Date = cer.Date,
                    IsLatest = cer.IsLatest,
                    Source = cer.Source,
                    CreatedAt = cer.CreatedAt,
                    UpdatedAt = cer.UpdatedAt
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / filter.PageSize);

            return new CurrencyExchangeRateListResponseViewModel
            {
                Success = true,
                Data = exchangeRates,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rates");
            return new CurrencyExchangeRateListResponseViewModel
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<CurrencyExchangeRateViewModel?> GetExchangeRateByIdAsync(int id)
    {
        try
        {
            var exchangeRate = await _context.CurrencyExchangeRates
                .Include(cer => cer.FromCurrency)
                .Include(cer => cer.ToCurrency)
                .FirstOrDefaultAsync(cer => cer.Id == id);

            if (exchangeRate == null)
                return null;

            return new CurrencyExchangeRateViewModel
            {
                Id = exchangeRate.Id,
                FromCurrencyId = exchangeRate.FromCurrencyId,
                FromCurrencyCode = exchangeRate.FromCurrency.Code,
                FromCurrencyName = exchangeRate.FromCurrency.Name,
                FromCurrencySymbol = exchangeRate.FromCurrency.Symbol,
                ToCurrencyId = exchangeRate.ToCurrencyId,
                ToCurrencyCode = exchangeRate.ToCurrency.Code,
                ToCurrencyName = exchangeRate.ToCurrency.Name,
                ToCurrencySymbol = exchangeRate.ToCurrency.Symbol,
                Rate = exchangeRate.Rate,
                Date = exchangeRate.Date,
                IsLatest = exchangeRate.IsLatest,
                Source = exchangeRate.Source,
                CreatedAt = exchangeRate.CreatedAt,
                UpdatedAt = exchangeRate.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate by id {Id}", id);
            return null;
        }
    }

    public async Task<LatestExchangeRateViewModel?> GetLatestExchangeRateAsync(int fromCurrencyId, int toCurrencyId)
    {
        try
        {
            var exchangeRate = await _context.CurrencyExchangeRates
                .Include(cer => cer.FromCurrency)
                .Include(cer => cer.ToCurrency)
                .Where(cer => cer.FromCurrencyId == fromCurrencyId && cer.ToCurrencyId == toCurrencyId && cer.IsLatest)
                .FirstOrDefaultAsync();

            if (exchangeRate == null)
                return null;

            return new LatestExchangeRateViewModel
            {
                FromCurrencyId = exchangeRate.FromCurrencyId,
                FromCurrencyCode = exchangeRate.FromCurrency.Code,
                ToCurrencyId = exchangeRate.ToCurrencyId,
                ToCurrencyCode = exchangeRate.ToCurrency.Code,
                Rate = exchangeRate.Rate,
                Date = exchangeRate.Date,
                Source = exchangeRate.Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest exchange rate for {FromId} to {ToId}", fromCurrencyId, toCurrencyId);
            return null;
        }
    }

    public async Task<LatestExchangeRateViewModel?> GetLatestExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode)
    {
        try
        {
            var exchangeRate = await _context.CurrencyExchangeRates
                .Include(cer => cer.FromCurrency)
                .Include(cer => cer.ToCurrency)
                .Where(cer => cer.FromCurrency.Code == fromCurrencyCode.ToUpper() && 
                              cer.ToCurrency.Code == toCurrencyCode.ToUpper() && 
                              cer.IsLatest)
                .FirstOrDefaultAsync();

            if (exchangeRate == null)
                return null;

            return new LatestExchangeRateViewModel
            {
                FromCurrencyId = exchangeRate.FromCurrencyId,
                FromCurrencyCode = exchangeRate.FromCurrency.Code,
                ToCurrencyId = exchangeRate.ToCurrencyId,
                ToCurrencyCode = exchangeRate.ToCurrency.Code,
                Rate = exchangeRate.Rate,
                Date = exchangeRate.Date,
                Source = exchangeRate.Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest exchange rate for {FromCode} to {ToCode}", fromCurrencyCode, toCurrencyCode);
            return null;
        }
    }

    public async Task<bool> SetLatestExchangeRateAsync(int fromCurrencyId, int toCurrencyId, decimal rate, string? source = null)
    {
        try
        {
            // Remove latest flag from existing rates for this currency pair
            var existingLatestRates = await _context.CurrencyExchangeRates
                .Where(cer => cer.FromCurrencyId == fromCurrencyId && 
                              cer.ToCurrencyId == toCurrencyId && 
                              cer.IsLatest)
                .ToListAsync();

            foreach (var existingRate in existingLatestRates)
            {
                existingRate.IsLatest = false;
                existingRate.UpdatedAt = DateTime.UtcNow;
            }

            // Create new latest exchange rate
            var newExchangeRate = new CurrencyExchangeRate
            {
                FromCurrencyId = fromCurrencyId,
                ToCurrencyId = toCurrencyId,
                Rate = rate,
                Date = DateTime.UtcNow.Date,
                IsLatest = true,
                Source = source,
                CreatedAt = DateTime.UtcNow
            };

            _context.CurrencyExchangeRates.Add(newExchangeRate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated latest exchange rate: {FromId} to {ToId} = {Rate}", fromCurrencyId, toCurrencyId, rate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting latest exchange rate");
            return false;
        }
    }

    public async Task<CurrencyConversionResponseViewModel> ConvertCurrencyAsync(CurrencyConversionRequestViewModel request)
    {
        try
        {
            // Handle same currency conversion
            if (request.FromCurrencyId == request.ToCurrencyId)
            {
                var sameCurrency = await _context.Currencies
                    .FirstOrDefaultAsync(c => c.Id == request.FromCurrencyId);

                if (sameCurrency == null)
                {
                    return new CurrencyConversionResponseViewModel
                    {
                        Success = false,
                        Error = "Currency not found"
                    };
                }

                return new CurrencyConversionResponseViewModel
                {
                    Success = true,
                    OriginalAmount = request.Amount,
                    FromCurrencyCode = sameCurrency.Code,
                    ConvertedAmount = request.Amount,
                    ToCurrencyCode = sameCurrency.Code,
                    ExchangeRate = 1.0m,
                    ExchangeRateDate = DateTime.UtcNow.Date,
                    Source = "Same Currency"
                };
            }

            // Get exchange rate
            var exchangeRate = await GetLatestExchangeRateAsync(request.FromCurrencyId, request.ToCurrencyId);

            if (exchangeRate == null)
            {
                return new CurrencyConversionResponseViewModel
                {
                    Success = false,
                    Error = "Exchange rate not found for the specified currency pair"
                };
            }

            var convertedAmount = request.Amount * exchangeRate.Rate;

            return new CurrencyConversionResponseViewModel
            {
                Success = true,
                OriginalAmount = request.Amount,
                FromCurrencyCode = exchangeRate.FromCurrencyCode,
                ConvertedAmount = convertedAmount,
                ToCurrencyCode = exchangeRate.ToCurrencyCode,
                ExchangeRate = exchangeRate.Rate,
                ExchangeRateDate = exchangeRate.Date,
                Source = exchangeRate.Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            return new CurrencyConversionResponseViewModel
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    // Implementing placeholder methods for the interface
    public async Task<CurrencyExchangeRateViewModel?> CreateExchangeRateAsync(CurrencyExchangeRateCreateEditViewModel model)
    {
        try
        {
            // Validate currencies exist
            var fromCurrency = await _context.Currencies.FindAsync(model.FromCurrencyId);
            var toCurrency = await _context.Currencies.FindAsync(model.ToCurrencyId);

            if (fromCurrency == null || toCurrency == null)
                return null;

            var exchangeRate = new CurrencyExchangeRate
            {
                FromCurrencyId = model.FromCurrencyId,
                ToCurrencyId = model.ToCurrencyId,
                Rate = model.Rate,
                Date = model.Date.Date,
                IsLatest = model.IsLatest,
                Source = model.Source,
                CreatedAt = DateTime.UtcNow
            };

            _context.CurrencyExchangeRates.Add(exchangeRate);
            await _context.SaveChangesAsync();

            return new CurrencyExchangeRateViewModel
            {
                Id = exchangeRate.Id,
                FromCurrencyId = exchangeRate.FromCurrencyId,
                FromCurrencyCode = fromCurrency.Code,
                FromCurrencyName = fromCurrency.Name,
                FromCurrencySymbol = fromCurrency.Symbol,
                ToCurrencyId = exchangeRate.ToCurrencyId,
                ToCurrencyCode = toCurrency.Code,
                ToCurrencyName = toCurrency.Name,
                ToCurrencySymbol = toCurrency.Symbol,
                Rate = exchangeRate.Rate,
                Date = exchangeRate.Date,
                IsLatest = exchangeRate.IsLatest,
                Source = exchangeRate.Source,
                CreatedAt = exchangeRate.CreatedAt,
                UpdatedAt = exchangeRate.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exchange rate");
            return null;
        }
    }

    public async Task<bool> UpdateExchangeRateAsync(CurrencyExchangeRateCreateEditViewModel model)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> DeleteExchangeRateAsync(int id)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return false;
    }

    public async Task<List<LatestExchangeRateViewModel>> GetAllLatestExchangeRatesAsync()
    {
        try
        {
            var latestRates = await _context.CurrencyExchangeRates
                .Include(cer => cer.FromCurrency)
                .Include(cer => cer.ToCurrency)
                .Where(cer => cer.IsLatest)
                .Select(cer => new LatestExchangeRateViewModel
                {
                    FromCurrencyId = cer.FromCurrencyId,
                    FromCurrencyCode = cer.FromCurrency.Code,
                    ToCurrencyId = cer.ToCurrencyId,
                    ToCurrencyCode = cer.ToCurrency.Code,
                    Rate = cer.Rate,
                    Date = cer.Date,
                    Source = cer.Source
                })
                .ToListAsync();

            return latestRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all latest exchange rates");
            return new List<LatestExchangeRateViewModel>();
        }
    }

    public async Task<List<LatestExchangeRateViewModel>> GetLatestExchangeRatesForCurrencyAsync(int currencyId)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return new List<LatestExchangeRateViewModel>();
    }

    public async Task<bool> UpdateLatestExchangeRatesAsync(List<CurrencyExchangeRateCreateEditViewModel> rates)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return false;
    }

    public async Task<decimal?> GetConversionRateAsync(int fromCurrencyId, int toCurrencyId, DateTime? date = null)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> ImportExchangeRatesAsync(List<CurrencyExchangeRateCreateEditViewModel> rates)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return false;
    }

    public async Task<int> CleanupOldExchangeRatesAsync(DateTime cutoffDate)
    {
        // Implementation would go here
        await Task.CompletedTask;
        return 0;
    }
}