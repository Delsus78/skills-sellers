using skills_sellers.Entities;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models.Extensions;

public static class CardsModelsExtensions
{
    public static void UpdateCard(this UpdateRequest model, Card card)
    {
        card.Name = model.Name;
        card.Description = model.Description;
        card.ImageUrl = model.ImageUrl;
    }
    
    public static Card CreateCard(this CreateRequest model)
    {
        return new Card
        {
            Name = model.Name,
            Description = model.Description,
            ImageUrl = model.ImageUrl
        };
    }
}