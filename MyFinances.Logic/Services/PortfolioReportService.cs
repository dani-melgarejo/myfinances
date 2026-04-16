using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class PortfolioReportService(
    ApplicationDbContext context, 
    ILogger<PortfolioReportService> logger,
    ICurrencyService currencyService) : IPortfolioReportService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<PortfolioReportService> _logger = logger;
    private readonly ICurrencyService _currencyService = currencyService;

    public async Task<IEnumerable<PortfolioReportViewModel>> GetPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null)
    {
        try
        {
            // Get target currency info
            var targetCurrency = await GetTargetCurrencyAsync(targetCurrencyId);

            var sql = @"
                DECLARE @fecha_inicio DATE = {0};
                DECLARE @fecha_fin DATE = {1};
                DECLARE @target_currency_id INT = {2};
                DECLARE @currency_code NVARCHAR(10) = {3};
                DECLARE @currency_symbol NVARCHAR(10) = {4};
                DECLARE @user_id NVARCHAR(450) = {5};
 
            WITH SP500Data AS (
                SELECT 
                    -- Para el valor inicial, buscar la fecha más cercana anterior a @fecha_inicio
                    (SELECT TOP 1 [Close] 
                     FROM stocks_data sd_inicial
                     INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id
                     WHERE a_inicial.Ticker = '^GSPC' 
                     AND sd_inicial.Date < @fecha_inicio
                     ORDER BY sd_inicial.Date DESC) AS SP500Inicial,
        
                    -- Para el valor final, buscar la fecha más cercana anterior o igual a @fecha_fin
                    (SELECT TOP 1 [Close] 
                     FROM stocks_data sd_final
                     INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id
                     WHERE a_final.Ticker = '^GSPC' 
                     AND sd_final.Date <= @fecha_fin
                     ORDER BY sd_final.Date DESC) AS SP500Final
            ),
            SP500Performance AS (
                SELECT 
                    SP500Inicial,
                    SP500Final,
                    CASE 
                        WHEN SP500Inicial > 0 THEN 
                            ((SP500Final - SP500Inicial) / SP500Inicial) * 100
                        ELSE 0 
                    END AS SP500Rendimiento
                FROM SP500Data
                WHERE SP500Inicial IS NOT NULL AND SP500Final IS NOT NULL
            ),
            -- CTE para obtener las tasas de cambio más recientes
            ExchangeRates AS (
                SELECT 
                    c1.Id as FromCurrencyId,
                    c2.Id as ToCurrencyId,
                    CASE 
                        -- Si es la misma moneda, tasa = 1
                        WHEN c1.Id = @target_currency_id THEN 1.0
                        -- Si la moneda origen es USD y destino es la target, usar tasa directa
                        WHEN c1.Code = 'USD' AND c2.Id = @target_currency_id THEN 
                            ISNULL((SELECT TOP 1 Rate 
                                   FROM currency_exchange_rates cer 
                                   WHERE cer.from_currency_id = c1.Id 
                                   AND cer.to_currency_id = c2.Id 
                                   AND cer.IsLatest = 1), 1.0)
                        -- Si la moneda destino es USD y origen es otra, usar tasa inversa
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN ISNULL((SELECT TOP 1 Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1), 0) > 0
                                THEN 1.0 / (SELECT TOP 1 Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1)
                                ELSE 1.0
                            END
                        -- Para conversiones indirectas (origen -> USD -> destino)
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    -- Convertir a USD primero, luego a moneda destino
                                    CASE 
                                        WHEN ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1), 0) > 0
                                        AND ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1), 0) > 0
                                        THEN (1.0 / (SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1)) *
                                             (SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1)
                                        ELSE 1.0
                                    END
                            END
                    END AS ExchangeRate
                FROM currencies c1
                CROSS JOIN currencies c2
                WHERE c2.Id = @target_currency_id
            ),
            RangeData AS (
                SELECT 
                    p.asset_id,
                    a.Ticker,
                    -- TenenciaInicial con conversión de moneda
                    (SELECT TOP 1 p_inicial.TotalPrice * ISNULL(er.ExchangeRate, 1.0)
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.asset_id = p.asset_id 
                     AND p_inicial.UserId = @user_id
                     AND p_inicial.Date < @fecha_inicio
                     ORDER BY p_inicial.Date DESC) AS TenenciaInicial,
        
                    -- TenenciaFinal con conversión de moneda
                    LAST_VALUE(p.TotalPrice * ISNULL(er_range.ExchangeRate, 1.0)) OVER (
                        PARTITION BY p.asset_id 
                        ORDER BY p.Date 
                        ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING
                    ) AS TenenciaFinal,
                    
                    -- Worth con conversión de moneda (suma de Worth = ganancia real del período)
                    SUM(p.Worth * ISNULL(er_range.ExchangeRate, 1.0)) OVER (PARTITION BY p.asset_id) AS TotalWorthPeriodo,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date) AS rn_first,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date DESC) AS rn_last
                FROM possessions p 
                INNER JOIN assets a ON p.asset_id = a.Id
                LEFT JOIN ExchangeRates er_range ON er_range.FromCurrencyId = ISNULL(p.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
            ),
            MovementsInRange AS (
                SELECT 
                    m.asset_id,
                    -- Movimientos con conversión de moneda
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END) AS Compras,
                    SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END) AS Ventas,
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) END) AS InversionNeta
                FROM movements m
                LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                WHERE m.UserId = @user_id
                AND m.Date >= @fecha_inicio AND m.Date <= @fecha_fin
                GROUP BY m.asset_id
            ),
            AssetSummary AS (
                SELECT 
                    rd.asset_id,
                    rd.Ticker,
                    ISNULL(rd.TenenciaInicial, 0) AS TenenciaInicial,
                    ISNULL(rd.TenenciaFinal, 0) AS TenenciaFinal,
                    rd.TotalWorthPeriodo AS GananciaPerdida,
                    ISNULL(mir.Compras, 0) AS Compras,
                    ISNULL(mir.Ventas, 0) AS Ventas,
                    ISNULL(mir.InversionNeta, 0) AS InversionNeta,
                    -- Porcentaje: si TenenciaFinal = 0, usar (TenenciaInicial + Compras), sino usar (TenenciaInicial + InversionNeta)
                    CASE 
                        WHEN ISNULL(rd.TenenciaFinal, 0) = 0 THEN
                            CASE 
                                WHEN ISNULL(rd.TenenciaInicial, 0) + ISNULL(mir.Compras, 0) > 0 THEN 
                                    (rd.TotalWorthPeriodo * 100) / (ISNULL(rd.TenenciaInicial, 0) + ISNULL(mir.Compras, 0))
                                ELSE 0
                            END
                        ELSE
                            CASE 
                                WHEN ISNULL(rd.TenenciaInicial, 0) + ISNULL(mir.InversionNeta, 0) > 0 THEN 
                                    (rd.TotalWorthPeriodo * 100) / (ISNULL(rd.TenenciaInicial, 0) + ISNULL(mir.InversionNeta, 0))
                                ELSE 0
                            END
                    END AS PorcentajeGananciaPerdida
                FROM RangeData rd
                LEFT JOIN MovementsInRange mir ON rd.asset_id = mir.asset_id
                WHERE rd.rn_first = 1
                AND (ISNULL(rd.TenenciaInicial, 0) > 0 or ISNULL(rd.TenenciaFinal, 0) > 0 or rd.TotalWorthPeriodo <> 0)
            )

            SELECT 
                'RESUMEN POR ACTIVO' AS TipoReporte,
                Ticker AS Ticker,
                TenenciaInicial AS TenenciaInicial,
                TenenciaFinal AS TenenciaFinal,
                TenenciaInicial + InversionNeta AS TenenciaInicialMasInversionNeta,
                GananciaPerdida AS GananciaPerdidaUsd,
                Compras AS Compras,
                Ventas AS Ventas,
                InversionNeta AS InversionNeta,
                PorcentajeGananciaPerdida AS PorcentajeGananciaPerdida,
                CAST(0 AS DECIMAL(18,2)) AS SP500Rendimiento,
                @currency_code AS CurrencyCode,
                @currency_symbol AS CurrencySymbol
            FROM AssetSummary

            UNION ALL

            SELECT 
                'TOTAL PORTAFOLIO' AS TipoReporte,
                'CONSOLIDADO' AS Ticker,
                SUM(TenenciaInicial) AS ValorInicial,
                SUM(TenenciaFinal) AS ValorFinal,
                SUM(TenenciaInicial) + SUM(InversionNeta) AS TenenciaInicialMasInversionNeta,
                SUM(GananciaPerdida) AS GananciaPerdidaUsd,
                SUM(Compras) AS Compras,
                SUM(Ventas) AS Ventas,
                SUM(InversionNeta) AS InversionNeta,
                CASE 
                    WHEN SUM(TenenciaInicial) + SUM(InversionNeta) > 0 THEN 
                        SUM(GananciaPerdida) * 100 / (SUM(TenenciaInicial) + SUM(InversionNeta))
                    ELSE 0 
                END AS PorcentajeGananciaPerdida,
                ROUND((SELECT TOP 1 SP500Rendimiento FROM SP500Performance), 2) AS SP500Rendimiento,
                @currency_code AS CurrencyCode,
                @currency_symbol AS CurrencySymbol
            FROM AssetSummary

            ORDER BY TipoReporte, Ticker";

            var results = await _context.Database
                .SqlQueryRaw<PortfolioReportViewModel>(sql, fechaInicio.Date, fechaFin.Date, targetCurrency.Id, targetCurrency.Code, targetCurrency.Symbol, userId)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando reporte de portafolio");
            throw;
        }
    }

    public async Task<IEnumerable<MonthlyPortfolioReportViewModel>> GetMonthlyPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null)
    {
        try
        {
            var targetCurrency = await GetTargetCurrencyAsync(targetCurrencyId);

            var sql = @"
            DECLARE @fecha_inicio DATE = {0};
            DECLARE @fecha_fin DATE = {1};
            DECLARE @target_currency_id INT = {2};
            DECLARE @currency_code NVARCHAR(10) = {3};
            DECLARE @currency_symbol NVARCHAR(10) = {4};
            DECLARE @user_id NVARCHAR(450) = {5};
 
            WITH ExchangeRates AS (
                SELECT 
                    c1.Id as FromCurrencyId,
                    c2.Id as ToCurrencyId,
                    CASE 
                        WHEN c1.Id = @target_currency_id THEN 1.0
                        WHEN c1.Code = 'USD' AND c2.Id = @target_currency_id THEN 
                            ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = c1.Id AND cer.to_currency_id = c2.Id AND cer.IsLatest = 1), 1.0)
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer.to_currency_id = c1.Id AND cer.IsLatest = 1), 0) > 0
                                THEN 1.0 / (SELECT TOP 1 Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer.to_currency_id = c1.Id AND cer.IsLatest = 1)
                                ELSE 1.0
                            END
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    CASE 
                                        WHEN ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1), 0) > 0
                                        AND ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1), 0) > 0
                                        THEN (1.0 / (SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1)) *
                                             (SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1)
                                        ELSE 1.0
                                    END
                            END
                    END AS ExchangeRate
                FROM currencies c1
                CROSS JOIN currencies c2
                WHERE c2.Id = @target_currency_id
            ),
            MonthlyData AS (
                SELECT 
                    FORMAT(p.Date, 'yyyy-MM') AS Mes,
                    MIN(p.Date) AS PrimerDiaMes,
                    MAX(p.Date) AS UltimoDiaMes,
                    DATEADD(DAY, -1, MIN(p.Date)) AS UltimoDiaMesAnterior
                FROM possessions p
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
                GROUP BY FORMAT(p.Date, 'yyyy-MM')
            ),
            SP500MonthlyData AS (
                SELECT 
                    md.Mes,
                    (SELECT TOP 1 [Close] FROM stocks_data sd_inicial INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id WHERE a_inicial.Ticker = '^GSPC' AND sd_inicial.Date < md.PrimerDiaMes ORDER BY sd_inicial.Date DESC) AS SP500Inicial,
                    (SELECT TOP 1 [Close] FROM stocks_data sd_final INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id WHERE a_final.Ticker = '^GSPC' AND sd_final.Date <= md.UltimoDiaMes ORDER BY sd_final.Date DESC) AS SP500Final
                FROM MonthlyData md
            ),
            PortfolioMonthlyData AS (
                SELECT 
                    md.Mes,
                    (SELECT TOP 1 SUM(p_inicial.TotalPrice * ISNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.UserId = @user_id AND p_inicial.Date <= md.UltimoDiaMesAnterior
                     GROUP BY p_inicial.Date ORDER BY p_inicial.Date DESC) AS TenenciaInicial,
                    (SELECT SUM(p_final.TotalPrice * ISNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_final
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(p_final.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_final.UserId = @user_id AND p_final.Date = md.UltimoDiaMes) AS TenenciaFinal,
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND FORMAT(m.Date, 'yyyy-MM') = md.Mes) AS Compras,
                    (SELECT SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND FORMAT(m.Date, 'yyyy-MM') = md.Mes) AS Ventas,
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND FORMAT(m.Date, 'yyyy-MM') = md.Mes) AS InversionNeta
                FROM MonthlyData md
            ),
            SP500Performance AS (
                SELECT sp.Mes, sp.SP500Inicial, sp.SP500Final,
                    CASE WHEN sp.SP500Inicial > 0 THEN ((sp.SP500Final - SP500Inicial) / sp.SP500Inicial) * 100 ELSE 0 END AS SP500Rendimiento
                FROM SP500MonthlyData sp
            ),
            MonthlyResults AS (
                SELECT 
                    pm.Mes,
                    ISNULL(pm.TenenciaInicial, 0) AS TenenciaInicial,
                    ISNULL(pm.TenenciaFinal, 0) AS TenenciaFinal,
                    ISNULL(pm.TenenciaFinal, 0) - ISNULL(pm.TenenciaInicial, 0) - ISNULL(pm.InversionNeta, 0) AS GananciaPerdidaUsd,
                    ISNULL(pm.Compras, 0) AS Compras,
                    ISNULL(pm.Ventas, 0) AS Ventas,
                    ISNULL(pm.InversionNeta, 0) AS InversionNeta,
                    CASE 
                        WHEN ISNULL(pm.TenenciaInicial, 0) + ISNULL(pm.InversionNeta, 0) > 0 THEN 
                            ((ISNULL(pm.TenenciaFinal, 0) - ISNULL(pm.TenenciaInicial, 0) - ISNULL(pm.InversionNeta, 0)) * 100) / 
                            (ISNULL(pm.TenenciaInicial, 0) + ISNULL(pm.InversionNeta, 0))
                        ELSE 0 
                    END AS PorcentajeGananciaPerdida,
                    ISNULL(sp.SP500Rendimiento, 0) AS SP500Rendimiento
                FROM PortfolioMonthlyData pm
                LEFT JOIN SP500Performance sp ON pm.Mes = sp.Mes
            )

            SELECT Mes, TenenciaInicial, TenenciaFinal, GananciaPerdidaUsd AS GananciaPerdidaUsd,
                Compras, Ventas, InversionNeta, PorcentajeGananciaPerdida, SP500Rendimiento,
                @currency_code AS CurrencyCode, @currency_symbol AS CurrencySymbol
            FROM MonthlyResults
            ORDER BY Mes";

            var results = await _context.Database
                .SqlQueryRaw<MonthlyPortfolioReportViewModel>(sql, fechaInicio.Date, fechaFin.Date, targetCurrency.Id, targetCurrency.Code, targetCurrency.Symbol, userId)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando reporte mensual de portafolio");
            throw;
        }
    }

    public async Task<IEnumerable<DailyPortfolioReportViewModel>> GetDailyPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin, string userId, int? targetCurrencyId = null)
    {
        try
        {
            var targetCurrency = await GetTargetCurrencyAsync(targetCurrencyId);

            var sql = @"
            DECLARE @fecha_inicio DATE = {0};
            DECLARE @fecha_fin DATE = {1};
            DECLARE @target_currency_id INT = {2};
            DECLARE @currency_code NVARCHAR(10) = {3};
            DECLARE @currency_symbol NVARCHAR(10) = {4};
            DECLARE @user_id NVARCHAR(450) = {5};
 
            -- CTE para obtener las tasas de cambio más recientes
            WITH ExchangeRates AS (
                SELECT 
                    c1.Id as FromCurrencyId,
                    c2.Id as ToCurrencyId,
                    CASE 
                        -- Si es la misma moneda, tasa = 1
                        WHEN c1.Id = @target_currency_id THEN 1.0
                        -- Si la moneda origen es USD y destino es la target, usar tasa directa
                        WHEN c1.Code = 'USD' AND c2.Id = @target_currency_id THEN 
                            ISNULL((SELECT TOP 1 Rate 
                                   FROM currency_exchange_rates cer 
                                   WHERE cer.from_currency_id = c1.Id 
                                   AND cer.to_currency_id = c2.Id 
                                   AND cer.IsLatest = 1), 1.0)
                        -- Si la moneda destino es USD y origen es otra, usar tasa inversa
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN ISNULL((SELECT TOP 1 Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1), 0) > 0
                                THEN 1.0 / (SELECT TOP 1 Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1)
                                ELSE 1.0
                            END
                        -- Para conversiones indirectas (origen -> USD -> destino)
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    -- Convertir a USD primero, luego a moneda destino
                                    CASE 
                                        WHEN ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1), 0) > 0
                                        AND ISNULL((SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1), 0) > 0
                                        THEN (1.0 / (SELECT TOP 1 Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1)) *
                                             (SELECT TOP 1 Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1)
                                        ELSE 1.0
                                    END
                            END
                    END AS ExchangeRate
                FROM currencies c1
                CROSS JOIN currencies c2
                WHERE c2.Id = @target_currency_id
            ),
            DailyData AS (
                SELECT DISTINCT
                    CAST(p.Date AS DATE) AS Fecha,
                    DATEADD(DAY, -1, CAST(p.Date AS DATE)) AS DiaAnterior
                FROM possessions p
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
            ),
            SP500DailyData AS (
                SELECT 
                    dd.Fecha,
                    (SELECT TOP 1 [Close] 
                     FROM stocks_data sd_inicial
                     INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id
                     WHERE a_inicial.Ticker = '^GSPC' 
                     AND sd_inicial.Date < @fecha_inicio
                     ORDER BY sd_inicial.Date DESC) AS SP500Inicial,
            
                    (SELECT TOP 1 [Close] 
                     FROM stocks_data sd_final
                     INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id
                     WHERE a_final.Ticker = '^GSPC' 
                     AND sd_final.Date <= dd.Fecha
                     ORDER BY sd_final.Date DESC) AS SP500Final
                FROM DailyData dd
            ),
            PortfolioDailyData AS (
                SELECT 
                    dd.Fecha,
                    -- TenenciaInicial con conversión de moneda
                    (SELECT SUM(p_inicial.TotalPrice * ISNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.UserId = @user_id AND p_inicial.Date = dd.DiaAnterior) AS TenenciaInicial,
                    
                    -- TenenciaFinal con conversión de moneda
                    (SELECT SUM(p_final.TotalPrice * ISNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_final
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(p_final.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_final.UserId = @user_id AND p_final.Date = dd.Fecha) AS TenenciaFinal,
                    
                    -- Movimientos con conversión de moneda
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND CAST(m.Date AS DATE) = dd.Fecha) AS Compras,
                    
                    (SELECT SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND CAST(m.Date AS DATE) = dd.Fecha) AS Ventas,
                    
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * ISNULL(er.ExchangeRate, 1.0) END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = ISNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND CAST(m.Date AS DATE) = dd.Fecha) AS InversionNeta
                FROM DailyData dd
            ),
            SP500Performance AS (
                SELECT 
                    sp.Fecha,
                    sp.SP500Inicial,
                    sp.SP500Final,
                    CASE 
                        WHEN sp.SP500Inicial > 0 THEN 
                            ((sp.SP500Final - sp.SP500Inicial) / sp.SP500Inicial) * 100
                        ELSE 0 
                    END AS SP500Rendimiento
                FROM SP500DailyData sp
            ),
            DailyResults AS (
                SELECT 
                    pd.Fecha,
                    ISNULL(pd.TenenciaInicial, 0) AS TenenciaInicial,
                    ISNULL(pd.TenenciaFinal, 0) AS TenenciaFinal,
                    ISNULL(pd.TenenciaFinal, 0) - ISNULL(pd.TenenciaInicial, 0) - ISNULL(pd.InversionNeta, 0) AS GananciaPerdidaUsd,
                    ISNULL(pd.Compras, 0) AS Compras,
                    ISNULL(pd.Ventas, 0) AS Ventas,
                    ISNULL(pd.InversionNeta, 0) AS InversionNeta,
                    CASE 
                        WHEN ISNULL(pd.TenenciaInicial, 0) + ISNULL(pd.InversionNeta, 0) > 0 THEN 
                            ((ISNULL(pd.TenenciaFinal, 0) - ISNULL(pd.TenenciaInicial, 0) - ISNULL(pd.InversionNeta, 0)) * 100) / 
                            (ISNULL(pd.TenenciaInicial, 0) + ISNULL(pd.InversionNeta, 0))
                        ELSE 
                            0 
                    END AS PorcentajeGananciaPerdida,
                    ISNULL(sp.SP500Rendimiento, 0) AS SP500Rendimiento
                FROM PortfolioDailyData pd
                LEFT JOIN SP500Performance sp ON pd.Fecha = sp.Fecha
            )

            SELECT 
                Fecha,
                TenenciaInicial AS TenenciaInicial,
                TenenciaFinal AS TenenciaFinal,
                GananciaPerdidaUsd AS GananciaPerdidaUsd,
                Compras AS Compras,
                Ventas AS Ventas,
                InversionNeta AS InversionNeta,
                PorcentajeGananciaPerdida,
                SP500Rendimiento,
                @currency_code AS CurrencyCode,
                @currency_symbol AS CurrencySymbol
            FROM DailyResults
            ORDER BY Fecha";

            var results = await _context.Database
                .SqlQueryRaw<DailyPortfolioReportViewModel>(sql, fechaInicio.Date, fechaFin.Date, targetCurrency.Id, targetCurrency.Code, targetCurrency.Symbol, userId)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando reporte diario de portafolio");
            throw;
        }
    }

    private async Task<(int Id, string Code, string Symbol)> GetTargetCurrencyAsync(int? targetCurrencyId)
    {
        if (targetCurrencyId.HasValue)
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(targetCurrencyId.Value);
            if (currency != null)
            {
                return (currency.Id, currency.Code, currency.Symbol);
            }
        }
        
        // Default to USD
        var usdCurrency = await _currencyService.GetDefaultCurrencyAsync();
        return (usdCurrency?.Id ?? 1, usdCurrency?.Code ?? "USD", usdCurrency?.Symbol ?? "$");
    }
}
