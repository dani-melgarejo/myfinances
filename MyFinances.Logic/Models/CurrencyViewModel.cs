using System.ComponentModel.DataAnnotations;

namespace MyFinances.Logic.Models;

public class CurrencyViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CurrencyFilterViewModel
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "Code";
    public string SortDirection { get; set; } = "asc";
}

public class CurrencyCreateEditViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Currency code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string Code { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Currency name is required")]
    [StringLength(50, ErrorMessage = "Currency name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Currency symbol is required")]
    [StringLength(5, ErrorMessage = "Currency symbol cannot exceed 5 characters")]
    public string Symbol { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CurrencyListResponseViewModel
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<CurrencyViewModel> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}