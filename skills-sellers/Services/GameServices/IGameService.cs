using skills_sellers.Entities;
using skills_sellers.Models;

namespace skills_sellers.Services.GameServices;

public interface IGameService
{
    GamesResponse GetGameOfTheDay(int userId);
    Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model);
    GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model);
}