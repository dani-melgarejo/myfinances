using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;

namespace MyFinances.Logic.Services;

public class StockQuoteService(
    ApplicationDbContext context,
    ILogger<StockQuoteService> logger) : IStockQuoteService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<StockQuoteService> _logger = logger;

    public async Task<StockQuoteResponseViewModel> GetStockQuotesAsync(string ticker, DateTime fechaDesde, DateTime fechaHasta)
    {
        try
        {
            if (string.IsNullOrEmpty(ticker))
            {
                return new StockQuoteResponseViewModel
                {
                    Success = false,
                    Error = "El ticker es requerido"
                };
            }

            // Validar fechas
            if (fechaDesde > fechaHasta)
            {
                return new StockQuoteResponseViewModel
                {
                    Success = false,
                    Error = "La fecha desde no puede ser mayor a la fecha hasta"
                };
            }

            // Obtener datos desde Yahoo Finance
            var yahooClient = new YahooClient();
            
            _logger.LogInformation($"Obteniendo cotizaciones para {ticker} desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}");
            
            var historicalData = await yahooClient.GetHistoricalDataAsync(ticker.ToUpper(), DataFrequency.Daily, fechaDesde);

            // Filtrar por el rango de fechas y convertir a nuestro modelo
            var quotes = historicalData
                // .Where(h => h.Date.Date >= fechaDesde.Date && h.Date.Date <= fechaHasta.Date)
                .OrderByDescending(h => h.Date)
                .Select(h => new StockQuoteResultViewModel
                {
                    Ticker = ticker.ToUpper(),
                    Date = h.Date.Date,
                    Close = Convert.ToDecimal(h.Close),
                    Open = h.Open != 0 ? Convert.ToDecimal(h.Open) : null,
                    High = h.High != 0 ? Convert.ToDecimal(h.High) : null,
                    Low = h.Low != 0 ? Convert.ToDecimal(h.Low) : null,
                    Volume = h.Volume
                })
                .ToList();

            _logger.LogInformation($"✅ Obtenidas {quotes.Count} cotizaciones para {ticker}");

            return new StockQuoteResponseViewModel
            {
                Success = true,
                Data = quotes,
                Total = quotes.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo cotizaciones para {ticker}");
            
            return new StockQuoteResponseViewModel
            {
                Success = false,
                Error = $"Error al obtener cotizaciones: {ex.Message}"
            };
        }
    }

    public async Task<bool> SaveQuotesToDatabaseAsync(string ticker, List<StockQuoteResultViewModel> quotes)
    {
        try
        {
            if (quotes == null || !quotes.Any())
                return false;

            // Buscar o crear el asset
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Ticker == ticker.ToUpper());

            if (asset == null)
            {
                // Crear nuevo asset si no existe
                asset = new Asset
                {
                    Ticker = ticker.ToUpper()
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Creado nuevo asset: {ticker.ToUpper()}");
            }

            // Get default currency (USD) - CurrencyId will be null for backward compatibility (null = USD)
            // This maintains backward compatibility as requested
            
            // Preparar datos de mercado
            var marketDataList = new List<MarketData>();

            foreach (var quote in quotes)
            {
                // Verificar si ya existe el registro
                var existingData = await _context.MarketData
                    .AnyAsync(md => md.AssetId == asset.Id && md.Date == quote.Date.Date);

                if (!existingData)
                {
                    var marketData = new MarketData
                    {
                        AssetId = asset.Id,
                        Date = quote.Date.Date,
                        Close = quote.Close,
                        CurrencyId = null // null = USD for backward compatibility
                    };

                    marketDataList.Add(marketData);
                }
            }

            if (marketDataList.Any())
            {
                await _context.MarketData.AddRangeAsync(marketDataList);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ {marketDataList.Count} cotizaciones guardadas en base de datos para {ticker}");
                return true;
            }
            else
            {
                _logger.LogInformation($"No hay nuevas cotizaciones para guardar para {ticker}");
                return true; // No es un error, solo que no hay datos nuevos
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error guardando cotizaciones para {ticker}");
            return false;
        }
    }
}