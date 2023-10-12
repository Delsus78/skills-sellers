using skills_sellers.Entities;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models.Extensions;

public static class CardsModelsExtensions
{
    public static Card CreateCard(this CardCreateRequest model) 
        => new()
        {
            Name = model.Name,
            Description = model.Description,
            Collection = model.Collection,
            Rarity = model.Rarity
        };

    public static CardResponse ToResponse(this Card card) 
        => new(card.Id, card.Name, card.Collection, card.Description, card.Rarity);
}