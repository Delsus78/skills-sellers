using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities;

[PrimaryKey("UserId")]
public class Stats
{
    public int UserId { get; set; }
    public int TotalFailedCardsCauseOfCharisme { get; set; }
    public int TotalMessagesSent { get; set; }
    public int TotalCreatiumMined { get; set; }
    public int TotalOrMined { get; set; }
    public int TotalBuildingsUpgraded { get; set; }
    public int TotalRocketLaunched { get; set; }
    public int TotalMealCooked { get; set; }
    public int TotalDoublonsEarned { get; set; }
    public int TotalLooseAtCharismeCasino { get; set; }
    public int TotalWinAtCharismeCasino { get; set; }

    // One to one
    public User User { get; set; }
}