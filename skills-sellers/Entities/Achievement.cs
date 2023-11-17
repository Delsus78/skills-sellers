using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities;

[PrimaryKey("UserId")]
public class Achievement
{
    public int UserId { get; set; }
    public User User { get; set; }
    
    // Achievements

    public int Each5CuisineLevels { get; set; }
    public int Each5SalleDeSportLevels { set; get; }
    public int Each5SpatioportLevels { get; set; }
    public int Each100RocketLaunched { get; set; }
    public int Each100Doublon { get; set; }
    public int Each10Cards { get; set; }
    public int Each25CasinoWin { set; get; }
    public int Each100MealCooked { get; set; }
    public int Each25kCreatium { get; set; }
    public int Each20kGold { get; set; }
    public int Each50FailCharism { get; set; }
    public int Each5CardsWithStat10 { get; set; }
    public int EachCardsFullStat { get; set; }
    public int EachCollectionsCompleted { get; set; }
}