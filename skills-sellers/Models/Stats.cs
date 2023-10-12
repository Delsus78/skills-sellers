namespace skills_sellers.Models;

public record StatsResponse(
    int TotalCards,
    IDictionary<string, int> TotalCardsByRarity,
    int TotalFailedCardsCauseOfCharisme,
    int TotalMessagesSent,
    IDictionary<string, int> TotalResourcesMined,
    int TotalCardWithAStatMaxed,
    int TotalBuildingsUpgraded,
    int TotalRocketLaunched,
    int TotalMealCooked
    );