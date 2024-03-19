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
        var totalCardsAtFull10 = userCards.Count(c => c.Competences.GotAllMaxed());
        var totalCollectionsCompleted = userCards.GroupBy(c => c.Card.GetCollectionName()).Count(g => g.Count() == g.First().Card.GetCollectionNumber());
        
        
        // add rank to stats
        CustomTuple totalCardsTuple = new("Nombre total de cartes",nbCards, ranks["TotalCards"]);
        CustomTuple totalFailedCardsCauseOfCharismeTuple = new("Nombre d'échecs dus au charisme lors d'explorations",stats.TotalFailedCardsCauseOfCharisme, ranks["TotalFailedCardsCauseOfCharisme"]);
        CustomTuple totalMessagesSentTuple = new("Nombre de messages envoyés", stats.TotalMessagesSent, ranks["TotalMessagesSent"]);
        CustomTuple totalCreatiumMinedTuple = new("Nombre de créatiums minés", stats.TotalCreatiumMined, ranks["TotalCreatiumMined"]);
        CustomTuple totalOrMinedTuple = new("Nombre d'onces d'or minées", stats.TotalOrMined, ranks["TotalOrMined"]);
        CustomTuple totalCardWithAStatMaxedTuple = new("Nombre de cartes avec une compétence maximisée", totalCardsWithAStatMaxed, ranks["TotalCardWithAStatMaxed"]);
        CustomTuple totalCardsAtFull10Tuple = new("Nombre de cartes avec toutes les compétences à 10", totalCardsAtFull10, ranks["TotalCardsAtFull10"]);
        CustomTuple totalBuildingsUpgradedTuple = new("Nombre d'améliorations de bâtiments effectuées", stats.TotalBuildingsUpgraded, ranks["TotalBuildingsUpgraded"]);
        CustomTuple totalWeaponsUpgradedTuple = new("Nombre d'arme améliorées", stats.TotalWeaponsUpgraded, ranks["TotalWeaponsUpgraded"]);
        CustomTuple totalRocketLaunchedTuple = new("Nombre de fusées lancées", stats.TotalRocketLaunched, ranks["TotalRocketLaunched"]);
        CustomTuple totalMealCookedTuple = new("Nombre de repas préparés", stats.TotalMealCooked, ranks["TotalMealCooked"]);
        CustomTuple totalDoublonsEarnedTuple = new("Nombre de doublons obtenus", stats.TotalDoublonsEarned, ranks["TotalDoublonsEarned"]);
        CustomTuple totalMachineUsedTuple = new("Nombre de machine E. Zeiss utilisées", stats.TotalMachineUsed, ranks["TotalMachineUsed"]);
        CustomTuple totalWordleWonTuple = new("Nombre de Wordle gagnés", stats.TotalWordleWon, ranks["TotalWordleWon"]);
        CustomTuple totalLooseAtCharismeCasinoTuple = new("Nombre d'or perdu au casino", stats.TotalLooseAtCharismeCasino, ranks["TotalLooseAtCharismeCasino"]);
        CustomTuple totalWinAtCharismeCasinoTuple = new("Nombre de charisme remporté au casino", stats.TotalWinAtCharismeCasino, ranks["TotalWinAtCharismeCasino"]);
        CustomTuple totalCollectionsCompletedTuple = new("Nombre de collection complétées", totalCollectionsCompleted, ranks["TotalCollectionsCompleted"]);
        
        var totalResourcesMinedTuples = new Dictionary<string, CustomTuple>
        {
            { "Creatium", totalCreatiumMinedTuple },
            { "Or", totalOrMinedTuple }
        };
        
        var totalCardsByRarityTuples = new Dictionary<string, CustomTuple>
        {
            { "commune", new("Nombre de cartes communes", nbCardsByRarity["commune"], ranks["TotalCardsCommune"]) },
            { "epic", new("Nombre de cartes épiques", nbCardsByRarity["epic"], ranks["TotalCardsEpic"]) },
            { "legendaire", new("Nombre de cartes légendaires", nbCardsByRarity["legendaire"], ranks["TotalCardsLegendaire"]) }
        };

        return new StatsResponse(
            totalCardsTuple,
            totalCardsByRarityTuples, 
            totalFailedCardsCauseOfCharismeTuple,
            totalMessagesSentTuple, 
            totalResourcesMinedTuples, 
            totalCardWithAStatMaxedTuple,
            totalBuildingsUpgradedTuple,
            totalWeaponsUpgradedTuple,
            totalRocketLaunchedTuple, 
            totalMealCookedTuple,
            totalDoublonsEarnedTuple,
            totalMachineUsedTuple,
            totalWordleWonTuple,
            totalLooseAtCharismeCasinoTuple,
            totalWinAtCharismeCasinoTuple,
            totalCardsAtFull10Tuple,
            totalCollectionsCompletedTuple,
            nbCardsInBDD);
    }

    
}