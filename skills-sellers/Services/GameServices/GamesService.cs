using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services.GameServices;

public class GamesService : IGameService
{
    private readonly CasinoService _casinoService;

    public GamesService(CasinoService casinoService)
    {
        _casinoService = casinoService;
    }
    public GamesResponse GetGameOfTheDay()
    {
        return DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Monday => _casinoService.GetGameOfTheDay(),
            DayOfWeek.Tuesday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Wednesday => _casinoService.GetGameOfTheDay(),
            DayOfWeek.Thursday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Friday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Saturday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Sunday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            _ => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400)
        };
    }

    public Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        return model.Name.ToLower() switch
        {
            "casino" => _casinoService.PlayGameOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        return model.Name.ToLower() switch
        {
            "casino" => _casinoService.EstimateGameOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }
}