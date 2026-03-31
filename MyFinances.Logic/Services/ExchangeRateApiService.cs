using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class ExchangeRateApiService(
    HttpClient httpClient,
    ICurrencyService currencyService,
    ICurrencyExchangeRateService currencyExchangeRateService,
    ILogger<ExchangeRateApiService> logger) : IExchangeRateApiService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ICurrencyService _currencyService = currencyService;
    private readonly ICurrencyExchangeRateService _currencyExchangeRateService = currencyExchangeRateService;
    private readonly ILogger<ExchangeRateApiService> _logger = logger;
    
    // API configuration
    private const string ApiKey = "ac27b4e67166f936e3441a7c";
    private const string BaseUrl = "https://v6.exchangerate-api.com/v6";

    public async Task<ExchangeRateApiResponse?> GetLatestExchangeRatesAsync(string baseCurrency = "USD")
    {
        try
        {
            var url = $"{BaseUrl}/{ApiKey}/latest/{baseCurrency}";
            
            _logger.LogInformation("Fetching exchange rates from: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };
            
            var apiResponse = JsonSerializer.Deserialize<ExchangeRateApiResponse>(jsonContent, options);
            
            if (apiResponse?.Result == "success")
            {
                _logger.LogInformation("Successfully fetched {Count} exchange rates", apiResponse.ConversionRates?.Count ?? 0);
                return apiResponse;
            }
            
            _logger.LogError("API returned error result: {Result}", apiResponse?.Result);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rates from API");
            throw;
        }
    }

    public async Task<ExchangeRateUpdateResponse> UpdateAllExchangeRatesAsync(ExchangeRateUpdateRequest? request = null)
    {
        request ??= new ExchangeRateUpdateRequest();
        
        var response = new ExchangeRateUpdateResponse
        {
            UpdatedAt = DateTime.UtcNow,
            Source = request.Source
        };

        try
        {
            // Get latest rates from API
            var apiResponse = await GetLatestExchangeRatesAsync("USD");
            if (apiResponse == null || apiResponse.ConversionRates == null)
            {
                response.Error = "Failed to fetch exchange rates from API";
                return response;
            }

            // Get all active currencies from our database
            var currencies = await _currencyService.GetActiveCurrenciesAsync();
            var currencyMap = currencies.ToDictionary(c => c.Code, c => c.Id);

            // Get USD currency ID (our base currency)
            var usdCurrency = currencies.FirstOrDefault(c => c.Code == "USD");
            if (usdCurrency == null)
            {
                response.Error = "USD currency not found in database";
                return response;
            }

            var usdCurrencyId = usdCurrency.Id;

            // Process each rate from the API
            foreach (var rate in apiResponse.ConversionRates)
            {
                try
                {
                    var toCurrencyCode = rate.Key;
                    var exchangeRate = rate.Value;

                    // Skip if we don't have this currency in our database
                    if (!currencyMap.ContainsKey(toCurrencyCode))
                    {
                        response.SkippedCurrencies.Add($"{toCurrencyCode} (not in database)");
                        response.RatesSkipped++;
                        continue;
                    }

                    // Skip if it's a specific currencies filter and not included
                    if (request.SpecificCurrencies != null && !request.SpecificCurrencies.Contains(toCurrencyCode))
                    {
                        response.SkippedCurrencies.Add($"{toCurrencyCode} (not in filter)");
                        response.RatesSkipped++;
                        continue;
                    }

                    var toCurrencyId = currencyMap[toCurrencyCode];

                    // Don't create USD to USD rate
                    if (toCurrencyCode == "USD")
                    {
                        response.SkippedCurrencies.Add("USD (base currency)");
                        response.RatesSkipped++;
                        continue;
                    }

                    // Check if latest rate already exists
                    var existingRate = await _currencyExchangeRateService.GetLatestExchangeRateAsync(usdCurrencyId, toCurrencyId);
                    
                    bool shouldUpdate = true;
                    if (existingRate != null)
                    {
                        // Check if it's from today and same rate
                        if (existingRate.Date.Date == DateTime.UtcNow.Date && 
                            Math.Abs(existingRate.Rate - exchangeRate) < 0.00000001m &&
                            !request.OverwriteExisting)
                        {
                            shouldUpdate = false;
                            response.SkippedCurrencies.Add($"{toCurrencyCode} (already up to date)");
                            response.RatesSkipped++;
                        }
                    }

                    if (shouldUpdate)
                    {
                        // Set the latest exchange rate
                        var success = await _currencyExchangeRateService.SetLatestExchangeRateAsync(
                            usdCurrencyId, 
                            toCurrencyId, 
                            exchangeRate, 
                            request.Source);

                        if (success)
                        {
                            response.ProcessedCurrencies.Add(toCurrencyCode);
                            if (existingRate != null)
                            {
                                response.RatesUpdated++;
                            }
                            else
                            {
                                response.NewRatesAdded++;
                            }
                            response.TotalRatesProcessed++;
                            
                            _logger.LogDebug("Updated exchange rate: USD to {Currency} = {Rate}", toCurrencyCode, exchangeRate);
                        }
                        else
                        {
                            response.ErrorMessages.Add($"Failed to update rate for {toCurrencyCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing rate for currency {Currency}", rate.Key);
                    response.ErrorMessages.Add($"Error processing {rate.Key}: {ex.Message}");
                }
            }

            response.Success = response.TotalRatesProcessed > 0;
            
            _logger.LogInformation(
                "Exchange rate update completed. Processed: {Processed}, New: {New}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
                response.TotalRatesProcessed, response.NewRatesAdded, response.RatesUpdated, 
                response.RatesSkipped, response.ErrorMessages.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange rates");
            response.Error = ex.Message;
            return response;
        }
    }

    public async Task<ExchangeRateUpdateResponse> UpdateSpecificExchangeRatesAsync(List<string> currencyCodes, string source = "ExchangeRate-API")
    {
        var request = new ExchangeRateUpdateRequest
        {
            Source = source,
            SpecificCurrencies = currencyCodes,
            OverwriteExisting = true
        };

        return await UpdateAllExchangeRatesAsync(request);
    }
}