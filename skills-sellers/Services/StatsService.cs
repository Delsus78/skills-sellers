using System.Collections.Concurrent;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

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
    void OnBuildingsUpgraded(int userId);
    Stats GetOrCreateStatsEntity(User user);
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

    public void OnPlanetDiscovered(int userId)
    {
        AddStat(userId, "TotalPlanetDiscovered");
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


    #region Helpers methods

    public Stats GetOrCreateStatsEntity(User user)
    {
        using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        if (user == null)
            throw new AppException("User not found", 404);
        
        var stats = context.Stats.FirstOrDefault(s => s.UserId == user.Id) ?? new Stats
        {
            TotalFailedCardsCauseOfCharisme = 0,
            TotalMessagesSent = 0,
            TotalCreatiumMined = 0,
            TotalOrMined = 0,
            TotalBuildingsUpgraded = 0,
            TotalRocketLaunched = 0,
            TotalMealCooked = 0
        };

        user.Stats = stats;
        return stats;
    }

    #endregion
}