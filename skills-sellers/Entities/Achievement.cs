using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities;

[PrimaryKey("UserId")]
public class Achievement
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int CardAtStat10 { get; set; }
    public int Doublon { get; set; }
    public int Each5Cuisine { get; set; }
    public int Each5SalleDeSport { get; set; }
    public int Each5Spatioport { get; set; }
    public int CardAtFull10 { get; set; }
    public int CharismCasinoWin { get; set; }
}