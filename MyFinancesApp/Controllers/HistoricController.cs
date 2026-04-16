using Microsoft.AspNetCore.Mvc;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;
using Microsoft.AspNetCore.Identity;
using MyFinances.Domain.Model;
using Microsoft.Extensions.Options;
using MyFinances.Logic.Configuration;

namespace MyFinancesApp.Controllers;

public class HistoricController(
    IAssetService assetService,
    IHistoricService historicService,
    IMarketDataService marketDataService,
    IPossessionService possessionService,
    IExchangeRateApiService exchangeRateApiService,
    IMovementService movementService,
    UserManager<ApplicationUser> userManager,
    IOptions<AppConfig> appConfig) : Controller
{
    private readonly IAssetService _assetService = assetService;
    private readonly IHistoricService _historicService = historicService;
    private readonly IMarketDataService _marketDataService = marketDataService;
    private readonly IPossessionService possessionService = possessionService;
    private readonly IExchangeRateApiService _exchangeRateApiService = exchangeRateApiService;
    private readonly IMovementService _movementService = movementService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly MarketHoursConfig _marketHours = appConfig.Value.MarketHours;

    [HttpGet]
    public IActionResult Index()
    {
        // Pasar información del mercado a la vista
        ViewBag.IsMarketOpen = _marketHours.IsMarketOpen();
        ViewBag.EffectiveDataDate = _marketHours.GetEffectiveDataDate();
        ViewBag.MarketOpenTimeUtc = _marketHours.OpenTimeUtc;
        ViewBag.MarketCloseTimeUtc = _marketHours.CloseTimeUtc;
        ViewBag.CurrentTimeUtc = DateTime.UtcNow;
        
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CompleteAllAssets()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var userAssetIds = await _movementService.GetAssetIdsByUserAsync(user.Id);
            var assets = await _assetService.GetAssetsByIdsAsync(userAssetIds);

            // Usar la fecha efectiva basada en el estado del mercado
            var effectiveDate = _marketHours.GetEffectiveDataDate();
            var isMarketOpen = _marketHours.IsMarketOpen();

            var statusMessage = isMarketOpen 
                ? $"Mercado ABIERTO - Trayendo datos hasta: {effectiveDate:yyyy-MM-dd} (día hábil anterior)" 
                : $"Mercado CERRADO - Trayendo datos hasta: {effectiveDate:yyyy-MM-dd} (incluye día actual)";

            foreach (var asset in assets)
            {
                var lastData = await _marketDataService.GetLastMarketDataAsync(asset.Id);

                if (lastData != null && lastData.Date >= effectiveDate)
                {
                    continue; // Skip if data is already up to date
                }

                DateTime dateFrom = new(2020, 1, 1);
                if (lastData != null)
                {
                    dateFrom = lastData.Date.AddDays(1).Date;
                }

                // El servicio usa automáticamente la fecha efectiva configurada
                await _historicService.GetPostsAsync(asset.Id, dateFrom, null);
            }

            foreach (var asset in assets)
            {
                await possessionService.UpdatePossessionsForAssetAsync(asset.Id, user.Id);
            }

            // Update currency information for all possessions to ensure consistency
            await possessionService.UpdateAllPossessionsCurrencyAsync();

            return Json(new 
            { 
                message = $"Datos históricos completados para todos los assets y posesiones actualizadas. {statusMessage}",
                marketStatus = new
                {
                    isOpen = isMarketOpen,
                    effectiveDate = effectiveDate.ToString("yyyy-MM-dd"),
                    currentTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    openTimeUtc = _marketHours.OpenTimeUtc,
                    closeTimeUtc = _marketHours.CloseTimeUtc
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateExchangeRates()
    {
        try
        {
            var request = new ExchangeRateUpdateRequest
            {
                Source = "ExchangeRate-API",
                OverwriteExisting = true
            };

            var result = await _exchangeRateApiService.UpdateAllExchangeRatesAsync(request);

            if (result.Success)
            {
                var message = $"Cotizaciones actualizadas correctamente. " +
                             $"Procesadas: {result.TotalRatesProcessed}, " +
                             $"Nuevas: {result.NewRatesAdded}, " +
                             $"Actualizadas: {result.RatesUpdated}, " +
                             $"Omitidas: {result.RatesSkipped}";

                if (result.ErrorMessages.Any())
                {
                    message += $". Errores: {result.ErrorMessages.Count}";
                }

                return Json(new 
                { 
                    message = message,
                    details = new
                    {
                        totalProcessed = result.TotalRatesProcessed,
                        newRates = result.NewRatesAdded,
                        updatedRates = result.RatesUpdated,
                        skippedRates = result.RatesSkipped,
                        errors = result.ErrorMessages.Count,
                        processedCurrencies = result.ProcessedCurrencies,
                        source = result.Source,
                        updatedAt = result.UpdatedAt
                    }
                });
            }
            else
            {
                var errorMessage = "Error actualizando cotizaciones";
                if (!string.IsNullOrEmpty(result.Error))
                {
                    errorMessage += $": {result.Error}";
                }

                if (result.ErrorMessages.Any())
                {
                    errorMessage += $". Errores adicionales: {string.Join(", ", result.ErrorMessages)}";
                }

                return BadRequest(new { error = errorMessage });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error actualizando cotizaciones: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSpecificExchangeRates([FromBody] List<string> currencyCodes)
    {
        try
        {
            if (currencyCodes == null || !currencyCodes.Any())
            {
                return BadRequest(new { error = "No se especificaron códigos de moneda" });
            }

            var result = await _exchangeRateApiService.UpdateSpecificExchangeRatesAsync(currencyCodes, "ExchangeRate-API");

            if (result.Success)
            {
                var message = $"Cotizaciones actualizadas para {string.Join(", ", currencyCodes)}. " +
                             $"Procesadas: {result.TotalRatesProcessed}";

                return Json(new 
                { 
                    message = message,
                    details = new
                    {
                        requestedCurrencies = currencyCodes,
                        processedCurrencies = result.ProcessedCurrencies,
                        totalProcessed = result.TotalRatesProcessed,
                        newRates = result.NewRatesAdded,
                        updatedRates = result.RatesUpdated,
                        skippedRates = result.RatesSkipped,
                        errors = result.ErrorMessages
                    }
                });
            }
            else
            {
                return BadRequest(new { error = result.Error ?? "Error actualizando cotizaciones específicas" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error actualizando cotizaciones específicas: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetExchangeRateStatus()
    {
        try
        {
            // This would require implementing a method to get the status
            // For now, return a simple response
            return Json(new 
            { 
                message = "Estado de cotizaciones disponible",
                lastUpdate = DateTime.UtcNow,
                apiStatus = "ExchangeRate-API disponible"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePossessionsCurrency()
    {
        try
        {
            await possessionService.UpdateAllPossessionsCurrencyAsync();
            return Json(new { message = "Información de moneda actualizada para todas las posesiones basándose en sus assets" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error actualizando monedas de posesiones: {ex.Message}" });
        }
    }
}