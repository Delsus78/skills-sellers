using skills_sellers.Entities;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models.Extensions;

public static class CardsModelsExtensions
{
    public static Card CreateCard(this CreateRequest model)
    {
        return new Card
        {
            Name = model.Name,
            Description = model.Description,
            ImageUrl = model.ImageUrl
        };
    }
    
    public static CardResponse ToResponse(this Card card)
    {
        return new CardResponse(card.Id, card.Name, card.ImageUrl, card.Description);
    }
}