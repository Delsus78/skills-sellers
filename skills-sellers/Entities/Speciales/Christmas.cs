using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities.Speciales;

[Table("christmas")]
[PrimaryKey("UserId")]
public class Christmas
{
    public int UserId { get; set; }
    public User User { get; set; }
    
    public List<int> DaysOpened { get; set; } = new();
}