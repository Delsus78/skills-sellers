namespace skills_sellers.Models;

public record AchievementResponse(
    CustomAchievementTuple Each5CuisineLevels,
    CustomAchievementTuple Each5SalleDeSportLevels,
    CustomAchievementTuple Each5SpatioportLevels,
    CustomAchievementTuple Each100RocketLaunched,
    CustomAchievementTuple Each100Doublon,
    CustomAchievementTuple Each10Cards,
    CustomAchievementTuple Each25CasinoWin,
    CustomAchievementTuple Each100MealCooked,
    CustomAchievementTuple Each25kCreatium,
    CustomAchievementTuple Each20kGold,
    CustomAchievementTuple Each50FailCharism,
    CustomAchievementTuple Each5CardsWithStat10,
    CustomAchievementTuple EachCardsFullStat,
    CustomAchievementTuple EachCollectionsCompleted,
    CustomAchievementTuple FirstPlanetAttack,
    CustomAchievementTuple SurviveToAnAttack);

public record AchievementRequest(
    string AchievementName);
    
public record CustomAchievementTuple(string Title, int Value, bool IsClaimable);