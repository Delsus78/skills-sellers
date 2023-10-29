using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class StatsModelsExtensions
{
    public static StatsResponse ToResponse(this Stats stats, IEnumerable<UserCard> cards, Dictionary<string, int> ranks, int nbCardsInBDD)
    {
        var userCards = cards.ToList();
        var nbCards = userCards.Count;
        var nbCardsByRarity = userCards
            .GroupBy(c => c.Card.Rarity)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // set 0 if no cards of this rarity
        nbCardsByRarity.TryAdd("commune", 0);
        nbCardsByRarity.TryAdd("epic", 0);
        nbCardsByRarity.TryAdd("legendaire", 0);

            var totalCardsWithAStatMaxed = userCards.Count(c => c.Competences.GotOneMaxed());
        
        // add rank to stats
        CustomTuple totalCardsTuple = new(nbCards, ranks["TotalCards"]);
        CustomTuple totalFailedCardsCauseOfCharismeTuple = new(stats.TotalFailedCardsCauseOfCharisme, ranks["TotalFailedCardsCauseOfCharisme"]);
        CustomTuple totalMessagesSentTuple = new(stats.TotalMessagesSent, ranks["TotalMessagesSent"]);
        CustomTuple totalCreatiumMinedTuple = new(stats.TotalCreatiumMined, ranks["TotalCreatiumMined"]);
        CustomTuple totalOrMinedTuple = new(stats.TotalOrMined, ranks["TotalOrMined"]);
        CustomTuple totalCardWithAStatMaxedTuple = new(totalCardsWithAStatMaxed, ranks["TotalCardWithAStatMaxed"]);
        CustomTuple totalBuildingsUpgradedTuple = new(stats.TotalBuildingsUpgraded, ranks["TotalBuildingsUpgraded"]);
        CustomTuple totalRocketLaunchedTuple = new(stats.TotalRocketLaunched, ranks["TotalRocketLaunched"]);
        CustomTuple totalMealCookedTuple = new(stats.TotalMealCooked, ranks["TotalMealCooked"]);
        CustomTuple totalDoublonsEarnedTuple = new(stats.TotalDoublonsEarned, ranks["TotalDoublonsEarned"]);
        CustomTuple totalMachineUsedTuple = new(stats.TotalMachineUsed, ranks["TotalMachineUsed"]);
        CustomTuple totalWordleWonTuple = new(stats.TotalWordleWon, ranks["TotalWordleWon"]);

        var totalResourcesMinedTuples = new Dictionary<string, CustomTuple>
        {
            { "Creatium", totalCreatiumMinedTuple },
            { "Or", totalOrMinedTuple }
        };
        
        var totalCardsByRarityTuples = new Dictionary<string, CustomTuple>
        {
            { "commune", new(nbCardsByRarity["commune"], ranks["TotalCardsCommune"]) },
            { "epic", new(nbCardsByRarity["epic"], ranks["TotalCardsEpic"]) },
            { "legendaire", new(nbCardsByRarity["legendaire"], ranks["TotalCardsLegendaire"]) }
        };

        return new StatsResponse(
            totalCardsTuple,
            totalCardsByRarityTuples, 
            totalFailedCardsCauseOfCharismeTuple,
            totalMessagesSentTuple, 
            totalResourcesMinedTuples, 
            totalCardWithAStatMaxedTuple,
            totalBuildingsUpgradedTuple, 
            totalRocketLaunchedTuple, 
            totalMealCookedTuple,
            totalDoublonsEarnedTuple,
            totalMachineUsedTuple,
            totalWordleWonTuple,
            nbCardsInBDD);
    }

    
}