namespace MyFinances.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("assets")]
public class Asset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column(TypeName = "varchar(10)")]
    public required string Ticker { get; set; }

    public override string ToString()
    {
        return $"Asset(Id={Id}, Ticker=\"{Ticker}\")";
    }

    public Dictionary<string, object?> ToDict()
    {
        return new Dictionary<string, object?>
    {
        { "id", Id },
        { "ticker", Ticker }
    };
    }
}
