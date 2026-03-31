namespace MyFinancesApp.Models;

public class CreateAssetViewModel
{
    public required string Ticker { get; set; }
    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencySymbol { get; set; }
}

public class EditAssetViewModel
{
    public int Id { get; set; }
    public required string Ticker { get; set; }
    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencySymbol { get; set; }
}
