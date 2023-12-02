using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;

namespace skills_sellers.Services.GameServices;

public class GamesService : IGameService
{
    private readonly CasinoService _casinoService;
    private readonly MachineRepairService _machineRepairService;
    private readonly WordleGameService _wordleGameService;

    public GamesService(
        CasinoService casinoService,
        MachineRepairService machineRepairService,
        WordleGameService wordleGameService)
    {
        _casinoService = casinoService;
        _machineRepairService = machineRepairService;
        _wordleGameService = wordleGameService;
    }
    public GamesResponse GetGameOfTheDay(int userId)
    {
        return DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Monday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Tuesday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Wednesday => _casinoService.GetGameOfTheDay(userId),
            DayOfWeek.Thursday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Friday => _machineRepairService.GetGameOfTheDay(userId),
            DayOfWeek.Saturday => _machineRepairService.GetGameOfTheDay(userId),
            DayOfWeek.Sunday => _casinoService.GetGameOfTheDay(userId),
            _ => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400)
        };
    }

    public Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        // check if its the game day
        var game = GetGameOfTheDay(user.Id);

        if (!string.Equals(game.Name, model.Name, StringComparison.CurrentCultureIgnoreCase))
            throw new AppException("Le jeu demandé n'est pas disponible aujourd'hui.", 400);
        
        return model.Name.ToLower() switch
        {
            "casino" => _casinoService.PlayGameOfTheDay(user, model),
            "machine" => _machineRepairService.PlayGameOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        // check if its the game day
        var game = GetGameOfTheDay(user.Id);
        
        if (!string.Equals(game.Name, model.Name, StringComparison.CurrentCultureIgnoreCase))
            throw new AppException("Le jeu demandé n'est pas disponible aujourd'hui.", 400);
        
        return model.Name.ToLower() switch
        {
            "casino" => _casinoService.EstimateGameOfTheDay(user, model),
            "machine" => _machineRepairService.EstimateGameOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }
    
    public GamesResponse GetWordle(int userId)
        => _wordleGameService.GetGameOfTheDay(userId);
    
    public Task<GamesPlayResponse> PlayWordle(User user, GamesRequest model)
        => _wordleGameService.PlayGameOfTheDay(user, model);
}