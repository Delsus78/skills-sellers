using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("cosmetics")]
public class Cosmetic
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Price { get; set; }
    public int Rarity { get; set; }
}