using skills_sellers.Entities;
using skills_sellers.Helpers;

namespace skills_sellers.Models.Extensions;

public static class UserBatimentExtensions
{
    public static UserBatimentResponse ToResponse(this UserBatimentData userBatiment)
    {
        return new UserBatimentResponse(
            userBatiment.CuisineLevel,
            userBatiment.SalleSportLevel,
            userBatiment.LaboLevel,
            userBatiment.SpatioPortLevel);
    }
}