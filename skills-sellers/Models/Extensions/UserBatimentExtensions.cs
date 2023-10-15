using skills_sellers.Entities;
using skills_sellers.Services;

namespace skills_sellers.Models.Extensions;

public static class UserBatimentExtensions
{
    public static UserBatimentResponse ToResponse(this UserBatimentData userBatiment, int actualSalleSportUsed, int actualLaboUsed, int actualSpatioPortUsed)
    {
        return new UserBatimentResponse(
            userBatiment.CuisineLevel,
            userBatiment.NbCuisineUsedToday,
            userBatiment.SalleSportLevel,
            actualSalleSportUsed,
            userBatiment.LaboLevel,
            actualLaboUsed,
            userBatiment.SpatioPortLevel,
            actualSpatioPortUsed,
            userBatiment.NbBuyMarchandToday,
            MarchandService.MaxBuyMarchandPerDay);
    }

    public static UserBatimentData UpdateUserBatimentData(this UserBatimentRequest userBatimentRequest, UserBatimentData userBatiment)
    {
        userBatiment.CuisineLevel = userBatimentRequest.CuisineLevel;
        userBatiment.NbCuisineUsedToday = userBatimentRequest.NbCuisineUsedToday;
        userBatiment.SalleSportLevel = userBatimentRequest.SalleSportLevel;
        userBatiment.LaboLevel = userBatimentRequest.LaboLevel;
        userBatiment.SpatioPortLevel = userBatimentRequest.SpatioPortLevel;
        userBatiment.NbBuyMarchandToday = userBatimentRequest.NbBuyMarchandToday;
        
        return userBatiment;
    }
}