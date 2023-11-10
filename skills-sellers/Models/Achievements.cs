namespace skills_sellers.Models;

public record AchievementResponse(
    CustomAchievementTuple CardAtStat10,
    CustomAchievementTuple Doublon,
    CustomAchievementTuple Each5Cuisine,
    CustomAchievementTuple Each5SalleDeSport,
    CustomAchievementTuple Each5Spatioport,
    CustomAchievementTuple CardAtFull10,
    CustomAchievementTuple CharismCasinoWin,
    CustomAchievementTuple Got100RocketLaunched);

public record AchievementRequest(
    string AchievementName);
    
public record CustomAchievementTuple(int Value, bool IsClaimable);