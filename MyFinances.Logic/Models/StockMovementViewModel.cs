namespace MyFinances.Logic.Models;

public class StockMovementViewModel
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string? Ticker { get; set; }
    public int Operation { get; set; }
    public DateTime Date { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public int Type { get; set; }
}