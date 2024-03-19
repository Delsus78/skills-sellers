using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities.Registres;

[PrimaryKey("UserId")]
public class UserRegistreInfo
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int HostileAttackWon { get; set; }
    public int HostileAttackLost { get; set; }
}