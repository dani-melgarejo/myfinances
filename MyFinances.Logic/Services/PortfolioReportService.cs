using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinances.Domain.Model;
using MyFinances.Logic.Interfaces;
using MyFinances.Logic.Models;

namespace MyFinances.Logic.Services;

public class PortfolioReportService : IPortfolioReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PortfolioReportService> _logger;

    public PortfolioReportService(ApplicationDbContext context, ILogger<PortfolioReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PortfolioReportViewModel>> GetPortfolioReportAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var sql = @"
                DECLARE @fecha_inicio DATE = {0};
                DECLARE @fecha_fin DATE = {1};
 
            WITH SP500Data AS (
                SELECT 
                    -- Para el valor inicial, buscar la fecha más cercana anterior a @fecha_inicio
                    (SELECT TOP 1 sd_inicial.[Close] 
                     FROM Stocks_Data sd_inicial
                     INNER JOIN Assets a_inicial ON sd_inicial.asset_id = a_inicial.Id
                     WHERE a_inicial.Ticker = '^GSPC' 
                     AND sd_inicial.Date < @fecha_inicio
                     ORDER BY sd_inicial.Date DESC) AS SP500Inicial,
        
                    -- Para el valor final, buscar la fecha más cercana anterior o igual a @fecha_fin
                    (SELECT TOP 1 sd_final.[Close] 
                     FROM Stocks_Data sd_final
                     INNER JOIN Assets a_final ON sd_final.asset_id = a_final.Id
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
            RangeData AS (
                SELECT 
                    p.asset_id,
                    a.Ticker,
                    -- TenenciaInicial: buscar el valor más reciente anterior a @fecha_inicio
                    (SELECT TOP 1 p_inicial.TotalPrice 
                     FROM Possessions p_inicial
                     WHERE p_inicial.asset_id = p.asset_id 
                     AND p_inicial.Date < @fecha_inicio
                     ORDER BY p_inicial.Date DESC) AS TenenciaInicial,
        
                    LAST_VALUE(p.TotalPrice) OVER (
                        PARTITION BY p.asset_id 
                        ORDER BY p.Date 
                        ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING
                    ) AS TenenciaFinal,
                    SUM(p.Worth) OVER (PARTITION BY p.asset_id) AS TotalWorthPeriodo,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date) AS rn_first,
                    ROW_NUMBER() OVER (PARTITION BY p.asset_id ORDER BY p.Date DESC) AS rn_last
                FROM Possessions p 
                INNER JOIN Assets a ON p.asset_id = a.Id
                WHERE p.Date >= @fecha_inicio AND p.Date <= @fecha_fin
            ),
            MovementsInRange AS (
                SELECT 
                    m.asset_id,
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price ELSE 0 END) AS Compras,
                    SUM(CASE WHEN m.Operation = 1 THEN m.Quantity * m.Price ELSE 0 END) AS Ventas,
                    SUM(CASE WHEN m.Operation = 0 THEN m.Quantity * m.Price ELSE -m.Quantity * m.Price END) AS InversionNeta
                FROM Movements m
                WHERE m.Date >= @fecha_inicio AND m.Date <= @fecha_fin
                GROUP BY m.asset_id
            ),
            AssetSummary AS (
                SELECT 
                    rd.asset_id,
                    rd.Ticker,
                    ISNULL(rd.TenenciaInicial, 0) AS TenenciaInicial, -- Agregué ISNULL por si no hay datos anteriores
                    rd.TenenciaFinal,
                    rd.TotalWorthPeriodo,
                    ISNULL(mir.Compras, 0) AS Compras,
                    ISNULL(mir.Ventas, 0) AS Ventas,
                    ISNULL(mir.InversionNeta, 0) AS InversionNeta,
                    CASE WHEN rd.TotalWorthPeriodo < 0 THEN
                       CASE 
                            WHEN ISNULL(rd.TenenciaInicial, 0) > 0 THEN 
                                -(ABS(rd.TotalWorthPeriodo) * 100 / ISNULL(rd.TenenciaInicial, 0))
                           ELSE 
                               0 
                       END
                    ELSE	
                       CASE 
                            WHEN ISNULL(mir.InversionNeta, 0) + ISNULL(rd.TenenciaInicial, 0) > 0 THEN 
                                rd.TotalWorthPeriodo * 100 / (ISNULL(mir.InversionNeta, 0) + ISNULL(rd.TenenciaInicial, 0))
                           ELSE 0 
                       END
                    END AS PorcentajeGananciaPerdida
                FROM RangeData rd
                LEFT JOIN MovementsInRange mir ON rd.asset_id = mir.asset_id
                WHERE rd.rn_first = 1
                AND (ISNULL(rd.TenenciaInicial, 0) > 0 or rd.TenenciaFinal > 0 or rd.TotalWorthPeriodo > 0 or rd.TotalWorthPeriodo < 0)
            )

            SELECT 
                'RESUMEN POR ACTIVO' AS TipoReporte,
                Ticker AS Ticker,
                TenenciaInicial,
                TenenciaFinal,
                TotalWorthPeriodo AS GananciaPerdidaUsd,
                Compras,
                Ventas,
                InversionNeta,
                PorcentajeGananciaPerdida AS PorcentajeGananciaPerdida,
                0 AS SP500Rendimiento
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
                ROUND((SELECT TOP 1 SP500Rendimiento FROM SP500Performance), 2) AS SP500Rendimiento
            FROM AssetSummary

            ORDER BY TipoReporte, Ticker";

            var results = await _context.Database
                .SqlQueryRaw<PortfolioReportViewModel>(sql, fechaInicio.Date, fechaFin.Date)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando reporte de portafolio");
            throw;
        }
    }
}
