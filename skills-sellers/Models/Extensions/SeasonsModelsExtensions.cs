using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class SeasonsModelsExtensions
{
    public static SeasonResponse ToResponse(this Season season, User? winner = null)
    {
        return new SeasonResponse(
            season.Id,
            season.StartedDate,
            season.StartedDate + season.Duration,
            season.EndedDate,
            winner?.Pseudo,
            winner?.Id,
            season.RawJsonPlayerData);
    }
}