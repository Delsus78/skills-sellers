using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class StatsModelsExtensions
{
    public static StatsResponse ToResponse(this Stats stats, IEnumerable<UserCard> cards)
    {
        var userCards = cards.ToList();
        var nbCards = userCards.Count;
        var nbCardsByRarity = userCards
            .GroupBy(c => c.Card.Rarity)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalResourcesMined = new Dictionary<string, int>
        {
            { "Creatium", stats.TotalCreatiumMined },
            { "Or", stats.TotalOrMined }
        };
        
        var totalCardsWithAStatMaxed = userCards.Count(c => c.Competences.GotOneMaxed());

        return new StatsResponse(nbCards, nbCardsByRarity, stats.TotalFailedCardsCauseOfCharisme,
            stats.TotalMessagesSent, totalResourcesMined, totalCardsWithAStatMaxed,
            stats.TotalBuildingsUpgraded, stats.TotalRocketLaunched, stats.TotalMealCooked,
            stats.TotalDoublonsEarned);
    }

    
}