namespace skills_sellers.Models;

public record StatsResponse(
    int TotalCards,
    IDictionary<string, int> TotalCardsByRarity,
    int TotalFailedCardsCauseOfCharisme,
    int TotalMessagesSent,
    IDictionary<string, int> TotalResourcesMined,
    int TotalPlanetDiscovered,
    int TotalCardWithAStatMaxed,
    int TotalBuildingsUpgraded,
    int TotalRocketLaunched,
    int TotalMealCooked
    );