namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("possessions")]
public class Possession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Column("asset_id")]
    [Required]
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public required Asset Asset { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Required]
    public decimal Quantity { get; set; } = 0;

    [Column("totalprice", TypeName = "decimal(10,3)")]
    [Required]
    public decimal TotalPrice { get; set; } = 0;

    [Column(TypeName = "decimal(12,5)")]
    [Required]
    public decimal Worth { get; set; } = 0;

    public override string ToString()
    {
        return $"Possession(AssetId={AssetId}, Date={Date}, Quantity={Quantity}, TotalPrice={TotalPrice}, Worth={Worth})";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
        {
            { "id", Id },
            { "date", Date.ToString("yyyy-MM-dd") },
            { "asset_id", AssetId },
            { "quantity", (double)Quantity },
            { "totalprice", (double)TotalPrice },
            { "worth", (double)Worth }
        };
    }
}