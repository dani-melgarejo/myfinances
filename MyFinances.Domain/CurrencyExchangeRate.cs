namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("currency_exchange_rates")]
public class CurrencyExchangeRate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("from_currency_id")]
    [Required]
    public int FromCurrencyId { get; set; }

    [ForeignKey(nameof(FromCurrencyId))]
    public Currency FromCurrency { get; set; } = null!;

    [Column("to_currency_id")]
    [Required]
    public int ToCurrencyId { get; set; }

    [ForeignKey(nameof(ToCurrencyId))]
    public Currency ToCurrency { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,8)")]
    public decimal Rate { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public bool IsLatest { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Source of the exchange rate (Yahoo, API, Manual, etc.)
    [Column(TypeName = "varchar(50)")]
    [MaxLength(50)]
    public string? Source { get; set; }

    public override string ToString()
    {
        return $"CurrencyExchangeRate(FromCurrencyId={FromCurrencyId}, ToCurrencyId={ToCurrencyId}, Rate={Rate}, Date={Date}, IsLatest={IsLatest})";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            { "id", Id },
            { "from_currency_id", FromCurrencyId },
            { "to_currency_id", ToCurrencyId },
            { "rate", Rate },
            { "date", Date.ToString("yyyy-MM-dd") },
            { "is_latest", IsLatest },
            { "source", Source },
            { "created_at", CreatedAt },
            { "updated_at", UpdatedAt }
        };
    }
}