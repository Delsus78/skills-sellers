using skills_sellers.Entities;
using skills_sellers.Models;

namespace skills_sellers.Services.Achievements;

public class AchievementContext
{
    private IAchievementStrategy? _strategy;

    public AchievementContext() => _strategy = null;

    public void SetStrategy(IAchievementStrategy strategy) => _strategy = strategy;

    public bool IsClaimable() => 
        _strategy?.IsClaimable() ?? throw new InvalidOperationException("Strategy is not set");

    public void Claim(User user)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Strategy is not set");
        _strategy.Claim(user);
    }
    
    public static IEnumerable<AchievementStrategy> GetAllStrategies(
        StatsResponse stats, Achievement achievement, UserBatimentResponse userBatiment)
    {
        yield return new AchievementEach5CuisineLevels(userBatiment.CuisineLevel, achievement);
        yield return new AchievementEach5SalleDeSportLevels(userBatiment.SalleSportLevel, achievement);
        yield return new AchievementEach5SpatioportLevels(userBatiment.SpatioPortLevel, achievement);
        yield return new AchievementEach100RocketLaunched(stats.TotalRocketLaunched.Stat, achievement);
        yield return new AchievementEach100Doublon(stats.TotalDoublonsEarned.Stat, achievement);
        yield return new AchievementEach10Cards(stats.TotalCards.Stat, achievement);
        yield return new AchievementEach25CasinoWin(stats.TotalWinAtCharismeCasino.Stat, achievement);
        yield return new AchievementEach100MealCooked(stats.TotalMealCooked.Stat, achievement);
        yield return new AchievementEach25kCreatium(stats.TotalResourcesMined["Creatium"].Stat, achievement);
        yield return new AchievementEach20kGold(stats.TotalResourcesMined["Or"].Stat, achievement);
        yield return new AchievementEach50FailCharism(stats.TotalFailedCardsCauseOfCharisme.Stat, achievement);
        yield return new AchievementEach5CardsWithStat10(stats.TotalCardWithAStatMaxed.Stat, achievement);
        yield return new AchievementEachCardsFullStat(stats.TotalCardsFull10.Stat, achievement);
        yield return new AchievementEachCollectionsCompleted(stats.TotalCollectionsCompleted.Stat, achievement);
        yield return new AchievementFirstPlanetAttack(stats.TotalPlanetAttacked.Stat, achievement);
        yield return new AchievementEach50SurvivedPlanetAttack(stats.TotalAttackSurvived.Stat, achievement);
    }
}