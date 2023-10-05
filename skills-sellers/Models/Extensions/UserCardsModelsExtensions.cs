using skills_sellers.Entities;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models.Extensions;

public static class UserCardsModelsExtensions
{
    public static UserCardResponse ToUserCardInActionResponse(this UserCard userCard)
        => new(userCard.Card.Id, userCard.Card.Name, userCard.Card.ImageUrl, userCard.Card.Description, 
            userCard.Card.Rarity, userCard.Competences.ToResponse());

    public static UserCardResponse ToResponse(this UserCard userCard)
        => new(userCard.Card.Id, userCard.Card.Name, userCard.Card.ImageUrl, userCard.Card.Description, 
            userCard.Card.Rarity, userCard.Competences.ToResponse(), userCard.Action?.ToResponse());
}