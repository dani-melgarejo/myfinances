namespace MyFinances.Logic.Models;

public class ExchangeRateApiResponse
{
    public string Result { get; set; } = string.Empty;
    public string Documentation { get; set; } = string.Empty;
    public string TermsOfUse { get; set; } = string.Empty;
    public long TimeLastUpdateUnix { get; set; }
    public string TimeLastUpdateUtc { get; set; } = string.Empty;
    public long TimeNextUpdateUnix { get; set; }
    public string TimeNextUpdateUtc { get; set; } = string.Empty;
    public string BaseCode { get; set; } = string.Empty;
    public Dictionary<string, decimal> ConversionRates { get; set; } = new Dictionary<string, decimal>();
}

public class ExchangeRateUpdateRequest
{
    public string Source { get; set; } = "ExchangeRate-API";
    public bool OverwriteExisting { get; set; } = true;
    public List<string>? SpecificCurrencies { get; set; }
}

public class ExchangeRateUpdateResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TotalRatesProcessed { get; set; }
    public int NewRatesAdded { get; set; }
    public int RatesUpdated { get; set; }
    public int RatesSkipped { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public List<string> ProcessedCurrencies { get; set; } = new List<string>();
    public List<string> SkippedCurrencies { get; set; } = new List<string>();
    public List<string> ErrorMessages { get; set; } = new List<string>();
}