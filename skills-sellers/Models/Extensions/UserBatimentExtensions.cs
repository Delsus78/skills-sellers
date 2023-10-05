using skills_sellers.Entities;
using skills_sellers.Helpers;

namespace skills_sellers.Models.Extensions;

public static class UserBatimentExtensions
{
    public static UserBatimentResponse ToResponse(this UserBatimentData userBatiment)
    {
        return new UserBatimentResponse(
            userBatiment.CuisineLevel,
            userBatiment.NbCuisineUsedToday,
            userBatiment.SalleSportLevel,
            userBatiment.LaboLevel,
            userBatiment.SpatioPortLevel);
    }
    
    public static UserBatimentData UpdateUserBatimentData(this UserBatimentRequest userBatimentRequest, UserBatimentData userBatiment)
    {
        userBatiment.CuisineLevel = userBatimentRequest.CuisineLevel;
        userBatiment.NbCuisineUsedToday = userBatimentRequest.NbCuisineUsedToday;
        userBatiment.SalleSportLevel = userBatimentRequest.SalleSportLevel;
        userBatiment.LaboLevel = userBatimentRequest.LaboLevel;
        userBatiment.SpatioPortLevel = userBatimentRequest.SpatioPortLevel;
        
        return userBatiment;
    }
}