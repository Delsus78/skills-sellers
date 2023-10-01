using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("users")]
public class User
{
    public int Id { get; set; }
    public string Pseudo { get; set; }

    // Many to many
    public List<UserCard> UserCards { get; } = new();
    
    public int Creatium { get; set; }
    public int Or { get; set; }
    
    // One to one
    public Stats Stats { get; set; }
    public UserBatimentData UserBatimentData { get; set; }
}