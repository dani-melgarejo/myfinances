namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("movements")]
public class Movement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public Asset? Asset { get; set; }

    public int Operation { get; set; }

    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal Price { get; set; }

    public int Type { get; set; }

    public override string ToString()
    {
        return $"Movements(AssetId={AssetId}, Operation={Operation}, Date={Date})";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            { "id", Id },
            { "asset_id", AssetId },
            { "operation", Operation },
            { "date", Date.ToString("yyyy-MM-dd") },
            { "quantity", Quantity },
            { "price", Price },
            { "type", Type }
        };
    }
}
