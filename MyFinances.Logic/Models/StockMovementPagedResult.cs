namespace MyFinances.Logic.Models;

public class StockMovementPagedResult
{
    public IEnumerable<StockMovementViewModel> Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalSold { get; set; }
    public decimal NetInvestment { get; set; }
}
