using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("cards")]
public class Card
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    
    // Many to many
    public List<User> Users { get; } = new();
}