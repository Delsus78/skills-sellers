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
    
    public (int min, int max) GetLimitsForForceStat(int forceLevel, string resourceType)
    {
        if (forceLevel is < 1 or > 10)
            throw new AppException("La force doit être comprise entre 1 et 10.", 400);
        
        resourceType = resourceType.ToLower();
        
        if (!Limits.TryGetValue(resourceType, out var levelLimits) || !levelLimits.TryGetValue(forceLevel, out var limits))
            throw new AppException("Le type de ressource ou le niveau de force est invalide.", 400);

        return limits;
    }
    
    public int GetRandomValueForForceStat(int forceLevel, string resourceType)
    {
        if (forceLevel is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(forceLevel));
        
        resourceType = resourceType.ToLower();
        
        if (!Limits.TryGetValue(resourceType, out var levelLimits) || !levelLimits.TryGetValue(forceLevel, out var limits))
            throw new AppException("Le type de ressource ou le niveau de force est invalide.", 400);

        return Random.Next(limits.min, limits.max);
    }
}