using skills_sellers.Entities;
using skills_sellers.Helpers;

namespace skills_sellers.Models.Extensions;

public static class AchievementsModelsExtensions
{
    public static AchievementResponse ToResponse(this Achievement achievement, List<string> claimableAchievementsName)
    {
        
        return new AchievementResponse(
            new CustomAchievementTuple(
                achievement.CardAtStat10, claimableAchievementsName.Contains("CardAtStat10")),
            new CustomAchievementTuple(
                achievement.Doublon, claimableAchievementsName.Contains("Doublon")),
            new CustomAchievementTuple(
                achievement.Each5Cuisine, claimableAchievementsName.Contains("Each5Cuisine")),
            new CustomAchievementTuple(
                achievement.Each5SalleDeSport, claimableAchievementsName.Contains("Each5SalleDeSport")),
            new CustomAchievementTuple(
                achievement.Each5Spatioport, claimableAchievementsName.Contains("Each5Spatioport")),
            new CustomAchievementTuple(
                achievement.CardAtFull10, claimableAchievementsName.Contains("CardAtFull10")),
            new CustomAchievementTuple(
                achievement.CharismCasinoWin, claimableAchievementsName.Contains("CharismCasinoWin")),
                new CustomAchievementTuple(achievement.Got100RocketLaunched, claimableAchievementsName.Contains("Got100RocketLaunched"))
        );
    }

    public static bool IsClaimable(this Achievement achievement, 
        int actualStat, AchievementRequest achievementReq, int requiredAmount = 1, int maxClaimable = 1)
    {
        var achievementProperty = achievement.GetType().GetProperty(achievementReq.AchievementName, 
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (achievementProperty == null) 
            throw new AppException($"Achievement {achievementReq.AchievementName} not found", 404);
        
        var achievementValue = (int) (achievementProperty.GetValue(achievement) ?? throw new InvalidOperationException());
        
        if (maxClaimable == 0)
            return false;

        // Si maxClaimable == -1, il n'y a pas de limite stricte au nombre de fois que l'achievement peut être revendiqué.
        // Cependant, nous prenons en compte la valeur actuelle de l'achievement (achievementValue) pour déterminer
        // si l'achievement peut encore être revendiqué. Par exemple, si actualStat est 10, requiredAmount est 5, et
        // achievementValue est 0, alors l'achievement peut être revendiqué jusqu'à 2 fois.
        if (maxClaimable == -1)
        {
            // Calculer combien de fois l'achievement peut être revendiqué basé sur actualStat et requiredAmount.
            int claimableTimes = actualStat / requiredAmount;
    
            // Retourner true si le nombre de fois que l'achievement a déjà été revendiqué est inférieur
            // au nombre de fois qu'il peut théoriquement être revendiqué.
            return achievementValue < claimableTimes;
        }


        if (achievementValue >= maxClaimable) 
            return false;

        return actualStat >= requiredAmount;
    }
    
    public static void ClaimAchievement(this Achievement achievement, AchievementRequest achievementReq)
    {
        var achievementProperty = achievement.GetType().GetProperty(achievementReq.AchievementName, 
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (achievementProperty == null) 
            throw new AppException($"Achievement {achievementReq.AchievementName} not found", 404);
        
        var achievementValue = (int) (achievementProperty.GetValue(achievement) ?? throw new InvalidOperationException());
        
        achievementProperty.SetValue(achievement, achievementValue + 1);
    }
}