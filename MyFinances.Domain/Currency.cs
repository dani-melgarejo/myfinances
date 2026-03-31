namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("currencies")]
public class Currency
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(3)")]
    [MaxLength(3)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varchar(50)")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varchar(5)")]
    [MaxLength(5)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public bool IsDefault { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public virtual ICollection<MarketData> MarketData { get; set; } = new List<MarketData>();
    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();
    public virtual ICollection<Possession> Possessions { get; set; } = new List<Possession>();
    
    // Exchange rate navigation properties
    public virtual ICollection<CurrencyExchangeRate> FromExchangeRates { get; set; } = new List<CurrencyExchangeRate>();
    public virtual ICollection<CurrencyExchangeRate> ToExchangeRates { get; set; } = new List<CurrencyExchangeRate>();

    public override string ToString()
    {
        return $"Currency(Id={Id}, Code=\"{Code}\", Name=\"{Name}\", Symbol=\"{Symbol}\", IsDefault={IsDefault})";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            { "id", Id },
            { "code", Code },
            { "name", Name },
            { "symbol", Symbol },
            { "isDefault", IsDefault },
            { "isActive", IsActive },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt }
        };
    }
}