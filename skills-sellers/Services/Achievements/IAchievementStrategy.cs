using skills_sellers.Entities;

namespace skills_sellers.Services.Achievements;

public interface IAchievementStrategy
{
    bool IsClaimable();
    void Claim(User user);
}

public abstract class AchievementStrategy : IAchievementStrategy
{

    // Name abstract property
    public abstract string Name { get; }

    protected int StatValue { get; set; }
    protected Achievement Achievement { get; set; }

    protected AchievementStrategy(int statValue, Achievement achievement)
    {
        StatValue = statValue;
        Achievement = achievement;
    }

    public abstract bool IsClaimable();
    public abstract void Claim(User user);
}