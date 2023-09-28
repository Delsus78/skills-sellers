using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;

public interface IStatsService
{
    void OnCardFailedCauseOfCharisme(int userId);
    void OnMessageSent(int userId);
    /* TODO surement à opti plus tard*/
    void OnCreatiumMined(int userId, int amount);
    void OnOrMined(int userId, int amount);
    void OnPlanetDiscovered(int userId);
    void OnRocketLaunched(int userId);
    void OnMealCooked(int userId);
    
    Stats GetOrCreateStatsEntity(User user);
}
public class StatsService : IStatsService
{
    private readonly DataContext _context;
    
    public StatsService(DataContext context)
    {
        _context = context;
    }
    
    public void OnCardFailedCauseOfCharisme(int userId)
    {
        
    }

    public void OnMessageSent(int userId)
    {
        throw new NotImplementedException();
    }

    public void OnCreatiumMined(int userId, int amount)
    {
        throw new NotImplementedException();
    }

    public void OnOrMined(int userId, int amount)
    {
        throw new NotImplementedException();
    }

    public void OnPlanetDiscovered(int userId)
    {
        throw new NotImplementedException();
    }

    public void OnRocketLaunched(int userId)
    {
        throw new NotImplementedException();
    }

    public void OnMealCooked(int userId)
    {
        throw new NotImplementedException();
    }


    #region Helpers methods

    public Stats GetOrCreateStatsEntity(User user)
    {
        if (user == null)
            throw new AppException("User not found", 404);
        
        var stats = _context.Stats.FirstOrDefault(s => s.UserId == user.Id) ?? new Stats
        {
            TotalFailedCardsCauseOfCharisme = 0,
            TotalMessagesSent = 0,
            TotalCreatiumMined = 0,
            TotalOrMined = 0,
            TotalPlanetDiscovered = 0,
            TotalBuildingsUpgraded = 0,
            TotalRocketLaunched = 0,
            TotalMealCooked = 0
        };

        user.Stats = stats;
        _context.SaveChanges();
        return stats;
    }

    #endregion
}