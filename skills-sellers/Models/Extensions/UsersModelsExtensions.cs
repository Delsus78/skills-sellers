using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class UsersModelsExtensions
{
    public static void UpdateUser(this UpdateRequest model, User user)
    {
        user.Pseudo = model.Pseudo;
    }
    
    public static User CreateUser(this CreateRequest model)
    {
        return new User
        {
            Pseudo = model.Pseudo
        };
    }
}