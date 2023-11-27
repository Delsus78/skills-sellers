using skills_sellers.Entities.Speciales;

namespace skills_sellers.Models.Extensions;

public static class SpecialesModelsExtensions
{
    public static ChristmasResponse ToResponse(this Christmas christmas)
    {
        return new ChristmasResponse(christmas.DaysOpened);
    }
}