using System.ComponentModel.DataAnnotations;

namespace MyFinances.Logic.Models;
public class MarketDataViewModel
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public required string Ticker { get; set; }
    public DateTime Date { get; set; }
    public decimal Close { get; set; }
}

public class EditMarketDataViewModel
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    public string? Ticker { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio de cierre debe ser mayor a 0")]
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