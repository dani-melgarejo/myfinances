using System.ComponentModel.DataAnnotations;

namespace MyFinances.Logic.Models;

public class CurrencyExchangeRateViewModel
{
    public int Id { get; set; }
    public int FromCurrencyId { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string FromCurrencyName { get; set; } = string.Empty;
    public string FromCurrencySymbol { get; set; } = string.Empty;
    public int ToCurrencyId { get; set; }
    public string ToCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyName { get; set; } = string.Empty;
    public string ToCurrencySymbol { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
    public bool IsLatest { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CurrencyExchangeRateCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "From currency is required")]
    public int FromCurrencyId { get; set; }

    [Required(ErrorMessage = "To currency is required")]
    public int ToCurrencyId { get; set; }

    [Required(ErrorMessage = "Exchange rate is required")]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Exchange rate must be greater than 0")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; } = DateTime.Today;

    public bool IsLatest { get; set; } = false;

    [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters")]
    public string? Source { get; set; }
}

public class CurrencyExchangeRateFilterViewModel
{
    public int? FromCurrencyId { get; set; }
    public int? ToCurrencyId { get; set; }
    public string? FromCurrencyCode { get; set; }
    public string? ToCurrencyCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsLatest { get; set; }
    public string? Source { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "Date";
    public string SortDirection { get; set; } = "desc";
}

public class CurrencyExchangeRateListResponseViewModel
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<CurrencyExchangeRateViewModel> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class LatestExchangeRateViewModel
{
    public int FromCurrencyId { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public int ToCurrencyId { get; set; }
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
    public string? Source { get; set; }
}

public class CurrencyConversionRequestViewModel
{
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public int FromCurrencyId { get; set; }
    
    [Required]
    public int ToCurrencyId { get; set; }
    
    public DateTime? Date { get; set; }
}

public class CurrencyConversionResponseViewModel
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public decimal OriginalAmount { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime ExchangeRateDate { get; set; }
    public string? Source { get; set; }
}