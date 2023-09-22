using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("users")]
public class User
{
    public int Id { get; set; }
    public string Pseudo { get; set; }

    // Many to many
    public List<Card> Cards { get; } = new();
}