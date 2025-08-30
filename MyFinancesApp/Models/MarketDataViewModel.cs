namespace MyFinancesApp.Models;

public class MarketDataViewModel
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string Ticker { get; set; }
    public DateTime Date { get; set; }
    public decimal Close { get; set; }
}

public class MarketDataPagedResult
{
    public IEnumerable<MarketDataViewModel> Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}