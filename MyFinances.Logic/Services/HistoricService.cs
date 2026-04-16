using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Configuration;
using MyFinances.Logic.Interfaces;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;

namespace MyFinances.Logic.Services;

public class HistoricService(
    ApplicationDbContext context,
    ILogger<HistoricService> logger,
    IOptions<AppConfig> appConfig) : IHistoricService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<HistoricService> _logger = logger;
    private readonly MarketHoursConfig _marketHours = appConfig.Value.MarketHours;

    public async Task GetPostsAsync(int assetId, DateTime dateFrom, DateTime? dateTo)
    {
        try
        {
            // Obtener el asset
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == assetId) ?? throw new Exception($"Asset con ID {assetId} no encontrado");

            // Ajustar la fecha 'hasta' según el estado del mercado
            var effectiveDateTo = dateTo ?? _marketHours.GetEffectiveDataDate();
            
            var isMarketOpen = _marketHours.IsMarketOpen();
            _logger.LogInformation($"Estado del mercado: {(isMarketOpen ? "ABIERTO" : "CERRADO")}. " +
                                 $"Fecha efectiva para datos: {effectiveDateTo:yyyy-MM-dd}");

            // Construir URL
            var yahooClient = new YahooClient();

            IEnumerable<HistoricalChartInfo> historicalData;
            try
            {
                historicalData = await yahooClient.GetHistoricalDataAsync(asset.Ticker, DataFrequency.Daily, dateFrom);
                
                // Filtrar datos hasta la fecha efectiva
                historicalData = historicalData.Where(h => h.Date.Date <= effectiveDateTo.Date);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Requested Information Not Available On Yahoo Finance"))
            {
                _logger.LogWarning($"No hay datos disponibles en Yahoo Finance para {asset.Ticker}. Se omite el asset.");
                return;
            }

            _logger.LogInformation($"Obteniendo datos históricos para {asset.Ticker} desde {dateFrom:yyyy-MM-dd} hasta {effectiveDateTo:yyyy-MM-dd}");

            var marketDataList = new List<MarketData>();
            foreach (HistoricalChartInfo row in historicalData)
            {
                // Verificar si ya existe el registro para evitar duplicados
                var existingData = await _context.MarketData
                    .AnyAsync(md => md.AssetId == assetId &&
                                  md.Date == row.Date.Date);

                if (!existingData)
                {
                    var marketData = new MarketData
                    {
                        AssetId = assetId,
                        Date = row.Date.Date,
                        Close = Convert.ToDecimal(row.Close)
                    };

                    marketDataList.Add(marketData);
                }
            }

            if (marketDataList.Any())
            {
                await _context.MarketData.AddRangeAsync(marketDataList);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ {marketDataList.Count} registros guardados para {asset.Ticker}");
            }
            else
            {
                _logger.LogInformation($"No hay nuevos datos para {asset.Ticker}");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo datos históricos para asset {assetId}");
            throw;
        }
    }
}