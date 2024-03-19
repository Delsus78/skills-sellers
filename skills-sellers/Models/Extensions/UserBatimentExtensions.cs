using skills_sellers.Entities;
using skills_sellers.Services;

namespace skills_sellers.Models.Extensions;

public static class UserBatimentExtensions
{
    public static UserBatimentResponse ToResponse(this UserBatimentData userBatiment,
        int actualSalleSportUsed = -1,
        int actualLaboUsed = -1,
        int actualSpatioPortUsed = -1,
        int actualSatelliteUsed = -1)
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
            userBatiment.SatelliteLevel,
            actualSatelliteUsed,
            MarchandService.MaxBuyMarchandPerDay);
    }

    public static UserBatimentData UpdateUserBatimentData(this UserBatimentRequest userBatimentRequest, UserBatimentData userBatiment)
    {
        userBatiment.CuisineLevel = userBatimentRequest.CuisineLevel;
        userBatiment.NbCuisineUsedToday = userBatimentRequest.NbCuisineUsedToday;
        userBatiment.SalleSportLevel = userBatimentRequest.SalleSportLevel;
        userBatiment.LaboLevel = userBatimentRequest.LaboLevel;
        userBatiment.SpatioPortLevel = userBatimentRequest.SpatioPortLevel;
        userBatiment.SatelliteLevel = userBatimentRequest.SatelliteLevel;
        userBatiment.NbBuyMarchandToday = userBatimentRequest.NbBuyMarchandToday;
        
        return userBatiment;
    }
}