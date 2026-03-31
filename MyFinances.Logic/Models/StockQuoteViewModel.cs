namespace MyFinances.Logic.Models;

public class StockQuoteRequestViewModel
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime FechaHasta { get; set; } = DateTime.Today;
}

public class StockQuoteResultViewModel
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Close { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public long? Volume { get; set; }
}

public class StockQuoteResponseViewModel
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<StockQuoteResultViewModel> Data { get; set; } = new();
    public int Total { get; set; }
}