using skills_sellers.Entities;
using skills_sellers.Helpers;
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
            Or = 20,
            Nourriture = 10,
            NbCardOpeningAvailable = 1,
            NbWeaponOpeningAvailable = 0,
            NbWeaponUpgradeAvailable = 0,
            WarTimeout = DateTime.Now.AddDays(1)
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
            user.Score,
            user.NbCardOpeningAvailable,
            user.UserCardsDoubled.Select(x => new CustomTupleDoublon(x.Id, x.CardId)).ToList(),
            user.NbWeaponOpeningAvailable,
            user.NbWeaponUpgradeAvailable,
            user.WarTimeout,
            user.CosmeticPoints());
    }
    
    public static int CosmeticPoints(this User user) 
        => user.Score / 100 - user.SpendedCosmeticPoints;

    public static Dictionary<string, int> GetResources(this User user)
    {
        return new Dictionary<string, int>
        {
            {"creatium", user.Creatium},
            {"or", user.Or},
            {"nourriture", user.Nourriture}
        };
    }
    
    // i.e. user.Resources["creatium"] = newValue;
    public static void SetResources(this User user, string resource, int value)
    {
        switch (resource)
        {
            case "creatium":
                user.Creatium = value;
                break;
            case "or":
                user.Or = value;
                break;
            case "nourriture":
                user.Nourriture = value;
                break;
            default:
                throw new AppException("Resource not found", 400);
        }
    }
}