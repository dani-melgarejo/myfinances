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
                SET @fecha_inicio = {0};
                SET @fecha_fin = {1};
                SET @target_currency_id = {2};
                SET @currency_code = {3};
                SET @currency_symbol = {4};
                SET @user_id = {5};
 
            WITH SP500Data AS (
                SELECT 
                    -- Para el valor inicial, buscar la fecha más cercana anterior a @fecha_inicio
                    (SELECT Close 
                     FROM stocks_data sd_inicial
                     INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id
                     WHERE a_inicial.Ticker = '^GSPC' 
                     AND sd_inicial.Date < @fecha_inicio
                     ORDER BY sd_inicial.Date DESC
                     LIMIT 1) AS SP500Inicial,
        
                    -- Para el valor final, buscar la fecha más cercana anterior o igual a @fecha_fin
                    (SELECT Close 
                     FROM stocks_data sd_final
                     INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id
                     WHERE a_final.Ticker = '^GSPC' 
                     AND sd_final.Date <= @fecha_fin
                     ORDER BY sd_final.Date DESC
                     LIMIT 1) AS SP500Final
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
                            IFNULL((SELECT Rate 
                                   FROM currency_exchange_rates cer 
                                   WHERE cer.from_currency_id = c1.Id 
                                   AND cer.to_currency_id = c2.Id 
                                   AND cer.IsLatest = 1
                                   LIMIT 1), 1.0)
                        -- Si la moneda destino es USD y origen es otra, usar tasa inversa
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN IFNULL((SELECT Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1
                                           LIMIT 1), 0) > 0
                                THEN 1.0 / (SELECT Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1
                                           LIMIT 1)
                                ELSE 1.0
                            END
                        -- Para conversiones indirectas (origen -> USD -> destino)
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    -- Convertir a USD primero, luego a moneda destino
                                    CASE 
                                        WHEN IFNULL((SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1), 0) > 0
                                        AND IFNULL((SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1 LIMIT 1), 0) > 0
                                        THEN (1.0 / (SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1)) *
                                             (SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1 LIMIT 1)
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
                    (SELECT p_inicial.TotalPrice * IFNULL(er.ExchangeRate, 1.0)
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.asset_id = p.asset_id 
                     AND p_inicial.UserId = @user_id
                     AND p_inicial.Date < @fecha_inicio
                     ORDER BY p_inicial.Date DESC
                     LIMIT 1) AS TenenciaInicial,
        
                    -- TenenciaFinal con conversión de moneda
                    LAST_VALUE(p.TotalPrice * IFNULL(er_range.ExchangeRate, 1.0)) OVER (
                        PARTITION BY p.asset_id 
                        ORDER BY p.Date 
                        ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING
                    ) AS TenenciaFinal,
                    
                    -- Worth con conversión de moneda
                    SUM(p.Worth * IFNULL(er_range.ExchangeRate, 1.0)) OVER (PARTITION BY p.asset_id) AS TotalWorthPeriodo,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date) AS rn_first,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date DESC) AS rn_last
                FROM possessions p 
                INNER JOIN assets a ON p.asset_id = a.Id
                LEFT JOIN ExchangeRates er_range ON er_range.FromCurrencyId = IFNULL(p.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
            ),
            MovementsInRange AS (
                SELECT 
                    m.asset_id,
                    -- Movimientos con conversión de moneda
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END) AS Compras,
                    SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END) AS Ventas,
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) END) AS InversionNeta
                FROM movements m
                LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                WHERE m.UserId = @user_id
                AND m.Date >= @fecha_inicio AND m.Date <= @fecha_fin
                GROUP BY m.asset_id
            ),
            AssetSummary AS (
                SELECT 
                    rd.asset_id,
                    rd.Ticker,
                    IFNULL(rd.TenenciaInicial, 0) AS TenenciaInicial,
                    rd.TenenciaFinal,
                    rd.TotalWorthPeriodo,
                    IFNULL(mir.Compras, 0) AS Compras,
                    IFNULL(mir.Ventas, 0) AS Ventas,
                    IFNULL(mir.InversionNeta, 0) AS InversionNeta,
                    CASE WHEN rd.TotalWorthPeriodo < 0 THEN
                       CASE 
                            WHEN IFNULL(rd.TenenciaInicial, 0) > 0 THEN 
                                -(ABS(rd.TotalWorthPeriodo) * 100 / IFNULL(rd.TenenciaInicial, 0))
                           ELSE 
                               0 
                       END
                    ELSE	
                       CASE 
                            WHEN IFNULL(mir.InversionNeta, 0) + IFNULL(rd.TenenciaInicial, 0) > 0 THEN 
                                rd.TotalWorthPeriodo * 100 / (IFNULL(mir.InversionNeta, 0) + IFNULL(rd.TenenciaInicial, 0))
                           ELSE 0 
                       END
                    END AS PorcentajeGananciaPerdida
                FROM RangeData rd
                LEFT JOIN MovementsInRange mir ON rd.asset_id = mir.asset_id
                WHERE rd.rn_first = 1
                AND (IFNULL(rd.TenenciaInicial, 0) > 0 or rd.TenenciaFinal > 0 or rd.TotalWorthPeriodo > 0 or rd.TotalWorthPeriodo < 0)
            )

            SELECT 
                'RESUMEN POR ACTIVO' AS TipoReporte,
                Ticker AS Ticker,
                TenenciaInicial AS TenenciaInicial,
                TenenciaFinal AS TenenciaFinal,
                TotalWorthPeriodo AS GananciaPerdidaUsd,
                Compras AS Compras,
                Ventas AS Ventas,
                InversionNeta AS InversionNeta,
                PorcentajeGananciaPerdida AS PorcentajeGananciaPerdida,
                0 AS SP500Rendimiento,
                @currency_code AS CurrencyCode,
                @currency_symbol AS CurrencySymbol
            FROM AssetSummary

            UNION ALL

            SELECT 
                'TOTAL PORTAFOLIO' AS TipoReporte,
                'CONSOLIDADO' AS Ticker,
                SUM(TenenciaInicial) AS ValorInicial,
                SUM(TenenciaFinal) AS ValorFinal,
                SUM(TotalWorthPeriodo) AS GananciaPerdidaUsd,
                SUM(Compras) AS Compras,
                SUM(Ventas) AS Ventas,
                SUM(InversionNeta) AS InversionNeta,
                CASE 
                    WHEN SUM(TenenciaInicial) + SUM(InversionNeta) > 0 THEN 
                        SUM(TotalWorthPeriodo) * 100 / (SUM(InversionNeta) + SUM(TenenciaInicial))
                    ELSE 0 
                END AS PorcentajeGananciaPerdida,
                ROUND((SELECT SP500Rendimiento FROM SP500Performance LIMIT 1), 2) AS SP500Rendimiento,
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
            SET @fecha_inicio = {0};
            SET @fecha_fin = {1};
            SET @target_currency_id = {2};
            SET @currency_code = {3};
            SET @currency_symbol = {4};
            SET @user_id = {5};
 
            WITH ExchangeRates AS (
                SELECT 
                    c1.Id as FromCurrencyId,
                    c2.Id as ToCurrencyId,
                    CASE 
                        WHEN c1.Id = @target_currency_id THEN 1.0
                        WHEN c1.Code = 'USD' AND c2.Id = @target_currency_id THEN 
                            IFNULL((SELECT Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = c1.Id AND cer.to_currency_id = c2.Id AND cer.IsLatest = 1 LIMIT 1), 1.0)
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN IFNULL((SELECT Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer.to_currency_id = c1.Id AND cer.IsLatest = 1 LIMIT 1), 0) > 0
                                THEN 1.0 / (SELECT Rate FROM currency_exchange_rates cer WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer.to_currency_id = c1.Id AND cer.IsLatest = 1 LIMIT 1)
                                ELSE 1.0
                            END
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    CASE 
                                        WHEN IFNULL((SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1), 0) > 0
                                        AND IFNULL((SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1 LIMIT 1), 0) > 0
                                        THEN (1.0 / (SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1)) *
                                             (SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currencyId AND cer2.IsLatest = 1 LIMIT 1)
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
                    DATE_FORMAT(p.Date, '%Y-%m') AS Mes,
                    MIN(p.Date) AS PrimerDiaMes,
                    MAX(p.Date) AS UltimoDiaMes,
                    DATE_SUB(MIN(p.Date), INTERVAL 1 DAY) AS UltimoDiaMesAnterior
                FROM possessions p
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
                GROUP BY DATE_FORMAT(p.Date, '%Y-%m')
            ),
            SP500MonthlyData AS (
                SELECT 
                    md.Mes,
                    (SELECT Close FROM market_data sd_inicial INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id WHERE a_inicial.Ticker = '^GSPC' AND sd_inicial.Date < md.PrimerDiaMes ORDER BY sd_inicial.Date DESC LIMIT 1) AS SP500Inicial,
                    (SELECT Close FROM market_data sd_final INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id WHERE a_final.Ticker = '^GSPC' AND sd_final.Date <= md.UltimoDiaMes ORDER BY sd_final.Date DESC LIMIT 1) AS SP500Final
                FROM MonthlyData md
            ),
            PortfolioMonthlyData AS (
                SELECT 
                    md.Mes,
                    (SELECT SUM(p_inicial.TotalPrice * IFNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.UserId = @user_id AND p_inicial.Date <= md.UltimoDiaMesAnterior
                     GROUP BY p_inicial.Date ORDER BY p_inicial.Date DESC LIMIT 1) AS TenenciaInicial,
                    (SELECT SUM(p_final.TotalPrice * IFNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_final
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(p_final.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_final.UserId = @user_id AND p_final.Date = md.UltimoDiaMes) AS TenenciaFinal,
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE_FORMAT(m.Date, '%Y-%m') = md.Mes) AS Compras,
                    (SELECT SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE_FORMAT(m.Date, '%Y-%m') = md.Mes) AS Ventas,
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) END)
                     FROM movements m LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE_FORMAT(m.Date, '%Y-%m') = md.Mes) AS InversionNeta
                FROM MonthlyData md
            ),
            SP500Performance AS (
                SELECT sp.Mes, sp.SP500Inicial, sp.SP500Final,
                    CASE WHEN sp.SP500Inicial > 0 THEN ((sp.SP500Final - sp.SP500Inicial) / sp.SP500Inicial) * 100 ELSE 0 END AS SP500Rendimiento
                FROM SP500MonthlyData sp
            ),
            MonthlyResults AS (
                SELECT 
                    pm.Mes,
                    IFNULL(pm.TenenciaInicial, 0) AS TenenciaInicial,
                    IFNULL(pm.TenenciaFinal, 0) AS TenenciaFinal,
                    IFNULL(pm.TenenciaFinal, 0) - IFNULL(pm.TenenciaInicial, 0) - IFNULL(pm.InversionNeta, 0) AS GananciaPerdidaUsd,
                    IFNULL(pm.Compras, 0) AS Compras,
                    IFNULL(pm.Ventas, 0) AS Ventas,
                    IFNULL(pm.InversionNeta, 0) AS InversionNeta,
                    CASE 
                        WHEN IFNULL(pm.TenenciaInicial, 0) + IFNULL(pm.InversionNeta, 0) > 0 THEN 
                            ((IFNULL(pm.TenenciaFinal, 0) - IFNULL(pm.TenenciaInicial, 0) - IFNULL(pm.InversionNeta, 0)) * 100) / 
                            (IFNULL(pm.TenenciaInicial, 0) + IFNULL(pm.InversionNeta, 0))
                        ELSE 0 
                    END AS PorcentajeGananciaPerdida,
                    IFNULL(sp.SP500Rendimiento, 0) AS SP500Rendimiento
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
            SET @fecha_inicio = {0};
            SET @fecha_fin = {1};
            SET @target_currency_id = {2};
            SET @currency_code = {3};
            SET @currency_symbol = {4};
            SET @user_id = {5};
 
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
                            IFNULL((SELECT Rate 
                                   FROM currency_exchange_rates cer 
                                   WHERE cer.from_currency_id = c1.Id 
                                   AND cer.to_currency_id = c2.Id 
                                   AND cer.IsLatest = 1
                                   LIMIT 1), 1.0)
                        -- Si la moneda destino es USD y origen es otra, usar tasa inversa
                        WHEN c2.Code = 'USD' AND c1.Id != (SELECT Id FROM currencies WHERE Code = 'USD') THEN
                            CASE 
                                WHEN IFNULL((SELECT Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1
                                           LIMIT 1), 0) > 0
                                THEN 1.0 / (SELECT Rate 
                                           FROM currency_exchange_rates cer 
                                           WHERE cer.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD')
                                           AND cer.to_currency_id = c1.Id 
                                           AND cer.IsLatest = 1
                                           LIMIT 1)
                                ELSE 1.0
                            END
                        -- Para conversiones indirectas (origen -> USD -> destino)
                        ELSE 
                            CASE 
                                WHEN c1.Code = 'USD' THEN 1.0
                                ELSE 
                                    -- Convertir a USD primero, luego a moneda destino
                                    CASE 
                                        WHEN IFNULL((SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1), 0) > 0
                                        AND IFNULL((SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currency_id AND cer2.IsLatest = 1 LIMIT 1), 0) > 0
                                        THEN (1.0 / (SELECT Rate FROM currency_exchange_rates cer1 WHERE cer1.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer1.to_currency_id = c1.Id AND cer1.IsLatest = 1 LIMIT 1)) *
                                             (SELECT Rate FROM currency_exchange_rates cer2 WHERE cer2.from_currency_id = (SELECT Id FROM currencies WHERE Code = 'USD') AND cer2.to_currency_id = @target_currencyId AND cer2.IsLatest = 1 LIMIT 1)
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
                    DATE(p.Date) AS Fecha,
                    DATE_SUB(DATE(p.Date), INTERVAL 1 DAY) AS DiaAnterior
                FROM possessions p
                WHERE p.UserId = @user_id
                AND p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
            ),
            SP500DailyData AS (
                SELECT 
                    dd.Fecha,
                    (SELECT Close 
                     FROM stocks_data sd_inicial
                     INNER JOIN assets a_inicial ON sd_inicial.asset_id = a_inicial.Id
                     WHERE a_inicial.Ticker = '^GSPC' 
                     AND sd_inicial.Date < dd.Fecha
                     ORDER BY sd_inicial.Date DESC
                     LIMIT 1) AS SP500Inicial,
            
                    (SELECT Close 
                     FROM stocks_data sd_final
                     INNER JOIN assets a_final ON sd_final.asset_id = a_final.Id
                     WHERE a_final.Ticker = '^GSPC' 
                     AND sd_final.Date = dd.Fecha
                     ORDER BY sd_final.Date DESC
                     LIMIT 1) AS SP500Final
                FROM DailyData dd
            ),
            PortfolioDailyData AS (
                SELECT 
                    dd.Fecha,
                    -- TenenciaInicial con conversión de moneda
                    (SELECT SUM(p_inicial.TotalPrice * IFNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_inicial
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(p_inicial.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_inicial.UserId = @user_id AND p_inicial.Date = dd.DiaAnterior) AS TenenciaInicial,
                    
                    -- TenenciaFinal con conversión de moneda
                    (SELECT SUM(p_final.TotalPrice * IFNULL(er.ExchangeRate, 1.0))
                     FROM possessions p_final
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(p_final.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE p_final.UserId = @user_id AND p_final.Date = dd.Fecha) AS TenenciaFinal,
                    
                    -- Movimientos con conversión de moneda
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE(m.Date) = dd.Fecha) AS Compras,
                    
                    (SELECT SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE 0 END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE(m.Date) = dd.Fecha) AS Ventas,
                    
                    (SELECT SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) ELSE -m.Quantity * m.Price * IFNULL(er.ExchangeRate, 1.0) END)
                     FROM movements m
                     LEFT JOIN ExchangeRates er ON er.FromCurrencyId = IFNULL(m.currency_id, (SELECT Id FROM currencies WHERE Code = 'USD'))
                     WHERE m.UserId = @user_id AND DATE(m.Date) = dd.Fecha) AS InversionNeta
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
                    IFNULL(pd.TenenciaInicial, 0) AS TenenciaInicial,
                    IFNULL(pd.TenenciaFinal, 0) AS TenenciaFinal,
                    IFNULL(pd.TenenciaFinal, 0) - IFNULL(pd.TenenciaInicial, 0) - IFNULL(pd.InversionNeta, 0) AS GananciaPerdidaUsd,
                    IFNULL(pd.Compras, 0) AS Compras,
                    IFNULL(pd.Ventas, 0) AS Ventas,
                    IFNULL(pd.InversionNeta, 0) AS InversionNeta,
                    CASE 
                        WHEN IFNULL(pd.TenenciaInicial, 0) + IFNULL(pd.InversionNeta, 0) > 0 THEN 
                            ((IFNULL(pd.TenenciaFinal, 0) - IFNULL(pd.TenenciaInicial, 0) - IFNULL(pd.InversionNeta, 0)) * 100) / 
                            (IFNULL(pd.TenenciaInicial, 0) + IFNULL(pd.InversionNeta, 0))
                        ELSE 0 
                    END AS PorcentajeGananciaPerdida,
                    IFNULL(sp.SP500Rendimiento, 0) AS SP500Rendimiento
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
