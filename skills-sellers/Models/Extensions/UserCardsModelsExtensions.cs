using skills_sellers.Entities;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models.Extensions;

public static class UserCardsModelsExtensions
{
    public static UserCardResponse ToUserCardResponse(this UserCard userCard) 
        => new(userCard.Card.Id, userCard.Card.Name, userCard.Card.ImageUrl, userCard.Card.Description,
            userCard.Competences.ToResponse());
}