using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities;

[PrimaryKey("UserId")]
public class WordleGame
{
    public int UserId { get; set; }
    public DateTime GameDate { get; set; }
    public bool? Win { get; set; }
    public List<string> Words { get; set; } = new();
    
    // One to one
    public User User { get; set; }
}