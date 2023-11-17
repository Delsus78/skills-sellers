using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class AchievementsModelsExtensions
{
    public static AchievementResponse ToResponse(this Achievement achievement, List<string> claimableAchievementsName)
    {
        
        return new AchievementResponse(
            new CustomAchievementTuple("Tous les 5 niveaux de cuisine",
                achievement.Each5CuisineLevels, claimableAchievementsName.Contains("Each5CuisineLevels")),
            new CustomAchievementTuple("Tous les 5 niveaux de salle de sport",
                achievement.Each5SalleDeSportLevels, claimableAchievementsName.Contains("Each5SalleDeSportLevels")),
            new CustomAchievementTuple("Tous les 5 niveaux de spatioport",
                achievement.Each5SpatioportLevels, claimableAchievementsName.Contains("Each5SpatioportLevels")),
            new CustomAchievementTuple("Tous les 100 Fusées lancées",
                achievement.Each100RocketLaunched, claimableAchievementsName.Contains("Each100RocketLaunched")),
            new CustomAchievementTuple("Tous les 100 Doublons",
                achievement.Each100Doublon, claimableAchievementsName.Contains("Each100Doublon")),
            new CustomAchievementTuple("Toutes les 10 cartes",
                achievement.Each10Cards, claimableAchievementsName.Contains("Each10Cards")),
            new CustomAchievementTuple("Toutes les 25 wins casino",
                achievement.Each25CasinoWin, claimableAchievementsName.Contains("Each25CasinoWin")),
            new CustomAchievementTuple("Tous les 100 repas",
                achievement.Each100MealCooked, claimableAchievementsName.Contains("Each100MealCooked")),
            new CustomAchievementTuple("Tous les 25k Creatium",
                achievement.Each25kCreatium, claimableAchievementsName.Contains("Each25kCreatium")),
            new CustomAchievementTuple("Tous les 20k Or",
                achievement.Each20kGold, claimableAchievementsName.Contains("Each20kGold")),
            new CustomAchievementTuple("Tous les 50 echecs due au charisme",
                achievement.Each50FailCharism, claimableAchievementsName.Contains("Each50FailCharism")),
            new CustomAchievementTuple("Toutes les 5 cartes avec une stat maxée",
                achievement.Each5CardsWithStat10, claimableAchievementsName.Contains("Each5CardsWithStat10")),
            new CustomAchievementTuple("Toutes les cartes full 10 stats",
                achievement.EachCardsFullStat, claimableAchievementsName.Contains("EachCardsFullStat")),
            new CustomAchievementTuple("Toutes les collections full",
                achievement.EachCollectionsCompleted, claimableAchievementsName.Contains("EachCollectionsCompleted"))
        );
    }
}