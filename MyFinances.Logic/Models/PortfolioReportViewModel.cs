namespace MyFinances.Logic.Models;

public class PortfolioReportViewModel
{
    public string TipoReporte { get; set; }
    public string Ticker { get; set; }
    public decimal TenenciaInicial { get; set; }
    public decimal TenenciaFinal { get; set; }
    public decimal GananciaPerdidaUsd { get; set; }
    public decimal Compras { get; set; }
    public decimal Ventas { get; set; }
    public decimal InversionNeta { get; set; }
    public decimal PorcentajeGananciaPerdida { get; set; }
    public decimal SP500Rendimiento { get; set; } // Nueva propiedad
}

public class PortfolioReportRequestViewModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime FechaFin { get; set; } = DateTime.Today;
}
