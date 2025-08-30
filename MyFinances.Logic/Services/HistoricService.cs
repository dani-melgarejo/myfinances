using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;

namespace MyFinances.Logic.Services;

public class HistoricService(
    ApplicationDbContext context,
    ILogger<HistoricService> logger) : IHistoricService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<HistoricService> _logger = logger;

    public async Task GetPostsAsync(int assetId, DateTime dateFrom, DateTime? dateTo)
    {
        try
        {
            // Obtener el asset
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == assetId) ?? throw new Exception($"Asset con ID {assetId} no encontrado");

            // Construir URL
            var yahooClient = new YahooClient();

            IEnumerable<HistoricalChartInfo> historicalData = await yahooClient.GetHistoricalDataAsync(asset.Ticker, DataFrequency.Daily, dateFrom);

            _logger.LogInformation($"Obteniendo datos históricos para {asset.Ticker} desde {dateFrom}");

            var marketDataList = new List<MarketData>();
            foreach (HistoricalChartInfo row in historicalData)
            {
                // Verificar si ya existe el registro para evitar duplicados
                var existingData = await _context.MarketData
                    .AnyAsync(md => md.AssetId == assetId &&
                                  md.Date == row.Date);

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