namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("stocks_data")]
public class MarketData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public Asset? Asset { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Close { get; set; }

    // Currency relationship - nullable for backward compatibility (null = USD)
    [Column("currency_id")]
    public int? CurrencyId { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public Currency? Currency { get; set; }

    public override string ToString()
    {
        return $"MarketData(AssetId={AssetId}, Date={Date}, Close={Close}, CurrencyId={CurrencyId})";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            { "id", Id },
            { "asset_id", AssetId },
            { "date", Date.ToString("yyyy-MM-dd") },
            { "close", Close },
            { "currency_id", CurrencyId },
        };
    }
}
