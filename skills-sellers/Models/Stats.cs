namespace skills_sellers.Models;

public record StatsResponse(
    CustomTuple TotalCards,
    IDictionary<string, CustomTuple> TotalCardsByRarity,
    CustomTuple TotalFailedCardsCauseOfCharisme,
    CustomTuple TotalMessagesSent,
    IDictionary<string, CustomTuple> TotalResourcesMined,
    CustomTuple TotalCardWithAStatMaxed,
    CustomTuple TotalBuildingsUpgraded,
    CustomTuple TotalRocketLaunched,
    CustomTuple TotalMealCooked,
    CustomTuple TotalDoublonsEarned,
    CustomTuple TotalMachineUsed,
    CustomTuple TotalWordleWon,
    CustomTuple TotalLooseAtCharismeCasino,
    CustomTuple TotalWinAtCharismeCasino,
    CustomTuple TotalCardsFull10,
    CustomTuple TotalCollectionsCompleted,
    int TotalCardsInBDD
);

public record CustomTuple(int Stat, int Rank);