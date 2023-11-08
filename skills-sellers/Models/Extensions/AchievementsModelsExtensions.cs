using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class AchievementsModelsExtensions
{
    public static AchievementResponse ToResponse(this Achievement achievement, List<string> claimableAchievementsName)
    {
        
        return new AchievementResponse(
            new CustomAchievementTuple(achievement.UserId, false),
            new CustomAchievementTuple(achievement.CardAtStat10, false),
            new CustomAchievementTuple(achievement.Doublon, false),
            new CustomAchievementTuple(achievement.Each5Cuisine, false),
            new CustomAchievementTuple(achievement.Each5SalleDeSport, false),
            new CustomAchievementTuple(achievement.Each5Spatioport, false),
            new CustomAchievementTuple(achievement.CardAtFull10, false),
            new CustomAchievementTuple(achievement.CharismCasinoWin, false)
        );
    }
}