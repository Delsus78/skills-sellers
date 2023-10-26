using skills_sellers.Entities;
using skills_sellers.Models.Users;

namespace skills_sellers.Models.Extensions;

public static class UsersModelsExtensions
{
    public static User CreateUser(this UserCreateRequest model)
    {
        return new User
        {
            Pseudo = model.Pseudo,
            Creatium = 600,
            Or = 0,
            Nourriture = 10,
            NbCardOpeningAvailable = 1,
            StatRepairedObjectMachine = -1
        };
    }
    
    public static UserResponse ToResponse(this User user)
    {
        var nbCards = user.UserCards.Count;
        return new UserResponse(user.Id,
            user.Pseudo,
            nbCards,
            user.Creatium,
            user.Or,
            user.Nourriture,
            user.NbCardOpeningAvailable,
            user.UserCardsDoubled.Select(x => x.CardId)
                .ToList(),
            user.StatRepairedObjectMachine);
    }
}