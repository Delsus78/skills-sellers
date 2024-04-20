using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;


public interface IResourcesService
{
    (int min, int max) GetLimitsForForceStat(int forceLevel, string resourceType);
    int GetRandomValueForForceStat(int forceLevel, string resourceType);
    
}

public class ResourcesService : IResourcesService
{
    private readonly Random Random = new();
    public static readonly Dictionary<string, Dictionary<int, (int min, int max)>> Limits = new()
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
                {0, (0, 0)},
                {1, (10, 20)},
                {2, (15, 25)},
                {3, (20, 30)},
                {4, (25, 40)},
                {5, (50, 70)},
                {6, (60, 80)},
                {7, (70, 100)},
                {8, (90, 130)},
                {9, (125, 190)},
                {10, (175, 200)}
            }
        }
    };
    
    public (int min, int max) GetLimitsForForceStat(int forceLevel, string resourceType)
    {
        
        resourceType = resourceType.ToLower();
        
        if (!Limits.TryGetValue(resourceType, out var levelLimits) || !levelLimits.TryGetValue(forceLevel, out var limits))
            throw new AppException("Le type de ressource ou le niveau de force est invalide.", 400);

        return limits;
    }
    
    public int GetRandomValueForForceStat(int forceLevel, string resourceType)
    {
        
        resourceType = resourceType.ToLower();
        
        if (!Limits.TryGetValue(resourceType, out var levelLimits) || !levelLimits.TryGetValue(forceLevel, out var limits))
            throw new AppException("Le type de ressource ou le niveau de force est invalide.", 400);

        return Random.Next(limits.min, limits.max);
    }
}
