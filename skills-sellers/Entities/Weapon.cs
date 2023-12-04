using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("weapons")]
public class Weapon
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}