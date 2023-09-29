using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;


public interface IResourcesService
{
    void AddResourcesToUser(int id, int creatium, int or);
    void TryRemoveResourcesToUser(int id, int creatium, int or);
    int GetRandomValueForForceStat(int forceLevel, string resourceType);
    int GetCreatiumCostForBuidlingUpgrade(int actualBuildingLevel);
}
public class ResourcesService : IResourcesService
{
    private readonly DataContext _context;
    private readonly Random Random = new();
    private readonly Dictionary<string, Dictionary<int, (int min, int max)>> Limits = new()
    {
        {
            "creatium", new Dictionary<int, (int max, int min)>
            {
                {1, (20, 50)},
                {2, (40, 100)},
                {3, (50, 110)},
                {4, (60, 135)},
                {5, (70, 150)},
                {6, (80, 170)},
                {7, (90, 185)},
                {8, (110, 200)},
                {9, (140, 225)},
                {10, (250, 350)}
            }
        },
        {
            "or", new Dictionary<int, (int max, int min)>
            {
                {1, (10, 20)},
                {2, (15, 25)},
                {3, (20, 30)},
                {4, (25, 40)},
                {5, (50, 70)},
                {6, (60, 80)},
                {7, (70, 100)},
                {8, (90, 150)},
                {9, (125, 200)},
                {10, (150, 250)}
            }
        }
    };
    
    public ResourcesService(DataContext context)
    {
        _context = context;
    }
    
    public void AddResourcesToUser(int id, int creatium, int or)
    {
        var user = _context.Users.Find(id);
        user.Creatium += creatium;
        user.Or += or;
        _context.Users.Update(user);
        _context.SaveChanges();
    }
    
    public void TryRemoveResourcesToUser(int id, int creatium, int or)
    {
        var user = _context.Users.Find(id);
        if (user.Creatium < creatium || user.Or < or)
            throw new AppException("Not enough resources", 400);
        user.Creatium -= creatium;
        user.Or -= or;
        _context.Users.Update(user);
        _context.SaveChanges();
    }
    
    public int GetRandomValueForForceStat(int forceLevel, string resourceType)
    {
        if (forceLevel is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(forceLevel));
        resourceType = resourceType.ToLower();
        if (!Limits.TryGetValue(resourceType, out var levelLimits) || !levelLimits.TryGetValue(forceLevel, out var limits))
            throw new AppException("Invalid resource type or force level.", 400);

        return Random.Next(limits.min, limits.max);
    }

    public int GetCreatiumCostForBuidlingUpgrade(int actualBuildingLevel)
        => (int) Math.Round(1.3 * actualBuildingLevel * 400);
}