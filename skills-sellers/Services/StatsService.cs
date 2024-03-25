using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface IStatsService
{
    ConcurrentDictionary<int, ConcurrentDictionary<string, int>> Stats { get; }
    void ResetStats();
    void OnCardFailedCauseOfCharisme(int userId);
    void OnMessageSent(int userId);
    void OnCreatiumMined(int userId, int amount);
    void OnOrMined(int userId, int amount);
    void OnRocketLaunched(int userId);
    void OnMealCooked(int userId);
    void OnMachineUsed(int userId);
    void OnWordleWin(int userId);
    void OnBuildingsUpgraded(int userId);
    void OnWeaponsUpgraded(int userId);
    void OnDoublonsEarned(int userId);
    void OnLooseGoldAtCharismeCasino(int userId, int amount);
    void OnWinAtCharismeCasino(int userId);
    void OnPlanetAttacked(int userId);
    void OnAttackSurvived(int userId);
    Stats GetOrCreateStatsEntity(User user);
    Dictionary<string, int> GetRanks(User user);
}
public class StatsService : IStatsService
{
    public ConcurrentDictionary<int, ConcurrentDictionary<string, int>> Stats { get; } = new();
    private readonly IServiceProvider _serviceProvider;
    
    public StatsService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void ResetStats()
    {
        Stats.Clear();
    }
    
    private void AddStat(int userId, string statName, int amount = 1)
    {
        if (!Stats.ContainsKey(userId))
            Stats.TryAdd(userId, new ConcurrentDictionary<string, int>());
        
        if (!Stats[userId].ContainsKey(statName))
            Stats[userId].TryAdd(statName, 0);
        
        Stats[userId][statName] += amount;
    }
    
    public void OnCardFailedCauseOfCharisme(int userId)
    {
        AddStat(userId, "TotalFailedCardsCauseOfCharisme");
    }

    public void OnMessageSent(int userId)
    {
        AddStat(userId, "TotalMessagesSent");
    }

    public void OnCreatiumMined(int userId, int amount)
    {
        AddStat(userId, "TotalCreatiumMined", amount);
    }

    public void OnOrMined(int userId, int amount)
    {
        AddStat(userId, "TotalOrMined", amount);
    }

    public void OnMachineUsed(int userId)
    {
        AddStat(userId, "TotalMachineUsed");
    }
    
    public void OnWordleWin(int userId)
    {
        AddStat(userId, "TotalWordleWon");
    }

    public void OnRocketLaunched(int userId)
    {
        AddStat(userId, "TotalRocketLaunched");
    }

    public void OnMealCooked(int userId)
    {
        AddStat(userId, "TotalMealCooked");
    }

    public void OnBuildingsUpgraded(int userId)
    {
        AddStat(userId, "TotalBuildingsUpgraded");
    }
    
    public void OnWeaponsUpgraded(int userId)
    {
        AddStat(userId, "TotalWeaponsUpgraded");
    }
    
    public void OnDoublonsEarned(int userId)
    {
        AddStat(userId, "TotalDoublonsEarned");
    }
    
    public void OnLooseGoldAtCharismeCasino(int userId, int amount)
    {
        AddStat(userId, "TotalLooseAtCharismeCasino", amount);
    }
    
    public void OnWinAtCharismeCasino(int userId)
    {
        AddStat(userId, "TotalWinAtCharismeCasino");
    }
    
    public void OnPlanetAttacked(int userId)
    {
        AddStat(userId, "TotalPlanetAttacked");
    }
    
    public void OnAttackSurvived(int userId)
    {
        AddStat(userId, "TotalAttackSurvived");
    }


    #region Helpers methods

    public Stats GetOrCreateStatsEntity(User user)
    {
        using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        if (user == null)
            throw new AppException("User not found", 404);
        
        var stats = context.Stats.FirstOrDefault(s => s.UserId == user.Id);

        if (stats != null) return stats;
        
        stats = new Stats
        {
            TotalFailedCardsCauseOfCharisme = 0,
            TotalMessagesSent = 0,
            TotalCreatiumMined = 0,
            TotalOrMined = 0,
            TotalBuildingsUpgraded = 0,
            TotalRocketLaunched = 0,
            TotalMealCooked = 0,
            TotalDoublonsEarned = 0,
            TotalMachineUsed = 0,
            TotalWordleWon = 0,
            TotalLooseAtCharismeCasino = 0,
            TotalWinAtCharismeCasino = 0
        };
        user.Stats = stats;
        return stats;
    }

    public Dictionary<string, int> GetRanks(User user)
    {
        var res = new Dictionary<string, int>();

        var statsCriteria = new List<Func<Stats, int>> {
            s => s.TotalFailedCardsCauseOfCharisme,
            s => s.TotalMessagesSent,
            s => s.TotalCreatiumMined,
            s => s.TotalOrMined,
            s => s.TotalBuildingsUpgraded,
            s => s.TotalWeaponsUpgraded,
            s => s.TotalRocketLaunched,
            s => s.TotalMealCooked,
            s => s.TotalDoublonsEarned,
            s => s.TotalMachineUsed,
            s => s.TotalLooseAtCharismeCasino,
            s => s.TotalWinAtCharismeCasino,
            s => s.TotalWordleWon,
            s => s.TotalPlanetAttacked,
            s => s.TotalAttackSurvived
        };

        var userCriteria = new List<Func<User, int>> {
            u => u.UserCards.Count,
            u => u.UserCards.Count(c => c.Competences.GotOneMaxed()),
            u => u.UserCards.Count(c => c.Competences.GotAllMaxed()),
            u => u.UserCards.GroupBy(c => c.Card.GetCollectionName()).Count(g => g.Count() == g.First().Card.GetCollectionNumber()),
            u => u.UserCards.Count(c => c.Card.Rarity == "commune"),
            u => u.UserCards.Count(c => c.Card.Rarity == "epic"),
            u => u.UserCards.Count(c => c.Card.Rarity == "legendaire"),
            u => u.UserCards.Count(c => c.Card.Rarity == "meethic")
        };

        var statsRanks = statsCriteria.AsParallel().Select(criterion => GetRankForStatCriteria(criterion, user.Id)).ToList();
        var userRanks = userCriteria.AsParallel().Select(criterion => GetRankForUserCriteria(criterion, user.Id)).ToList();

        var criteriaNames = new List<string> {
            "TotalFailedCardsCauseOfCharisme",
            "TotalMessagesSent",
            "TotalCreatiumMined",
            "TotalOrMined",
            "TotalBuildingsUpgraded",
            "TotalWeaponsUpgraded",
            "TotalRocketLaunched",
            "TotalMealCooked",
            "TotalDoublonsEarned",
            "TotalMachineUsed",
            "TotalLooseAtCharismeCasino",
            "TotalWinAtCharismeCasino",
            "TotalWordleWon",
            "TotalPlanetAttacked",
            "TotalAttackSurvived",
            "TotalCards",
            "TotalCardWithAStatMaxed",
            "TotalCardsAtFull10",
            "TotalCollectionsCompleted",
            "TotalCardsCommune",
            "TotalCardsEpic",
            "TotalCardsLegendaire",
            "TotalCardsMeethic"
        };

        var combinedRanks = statsRanks.Concat(userRanks).ToList();

        for (int index = 0; index < combinedRanks.Count; index++)
        {
            res[criteriaNames[index]] = combinedRanks[index];
        }

        return res;
    }

    private int GetRankForStatCriteria(Func<Stats, int> criterion, int userId)
    {
        using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var orderedList = context.Stats.OrderByDescending(criterion).ToList();
        return orderedList.FindIndex(x => x.UserId == userId) + 1;
    }

    private int GetRankForUserCriteria(Func<User, int> criterion, int userId)
    {
        using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var orderedList = context.Users
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Competences)
            .OrderByDescending(criterion).ToList();
        return orderedList.FindIndex(x => x.Id == userId) + 1;
    }




    #endregion
}