namespace skills_sellers.Models;

public record StatsResponse(
    CustomTuple TotalCards,
    IDictionary<string, CustomTuple> TotalCardsByRarity,
    CustomTuple TotalFailedCardsCauseOfCharisme,
    CustomTuple TotalMessagesSent,
    IDictionary<string, CustomTuple> TotalResourcesMined,
    CustomTuple TotalCardWithAStatMaxed,
    CustomTuple TotalBuildingsUpgraded,
    CustomTuple TotalWeaponsUpgraded,
    CustomTuple TotalRocketLaunched,
    CustomTuple TotalMealCooked,
    CustomTuple TotalDoublonsEarned,
    CustomTuple TotalMachineUsed,
    CustomTuple TotalWordleWon,
    CustomTuple TotalLooseAtCharismeCasino,
    CustomTuple TotalWinAtCharismeCasino,
    CustomTuple TotalCardsFull10,
    CustomTuple TotalCollectionsCompleted,
    CustomTuple TotalPlanetAttacked,
    CustomTuple TotalAttackSurvived,
    int TotalCardsInBDD
);

public record CustomTuple(string Title, int Stat, int Rank);