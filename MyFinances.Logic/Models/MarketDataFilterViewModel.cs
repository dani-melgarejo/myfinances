namespace MyFinances.Logic.Models;

public class MarketDataFilterViewModel
{
    public int? AssetId { get; set; }
    public string Ticker { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "Date";
    public string SortDirection { get; set; } = "desc";
}