using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class UserCardsModelsExtensions
{
    public static UserCardResponse ToUserCardInActionResponse(this UserCard userCard)
        => new(userCard.Card.Id, userCard.Card.Name, userCard.Card.Collection, userCard.Card.Description, 
            userCard.Card.Rarity, userCard.Competences.ToResponse(), 
            userCard.Competences.GetPowerWithoutWeapon() + userCard.UserWeapon?.Power ?? 0,
            null,
            userCard.UserWeapon?.ToResponse());

    public static UserCardResponse ToResponse(this UserCard userCard)
        => new(userCard.Card.Id, 
            userCard.Card.Name, 
            userCard.Card.Collection, 
            userCard.Card.Description, 
            userCard.Card.Rarity, 
            userCard.Competences.ToResponse(), 
            userCard.Competences.GetPowerWithoutWeapon() + (userCard.UserWeapon?.Power ?? 0), 
            userCard.Action?.ToResponse(), 
            userCard.UserWeapon?.ToResponse());
}