using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;

namespace skills_sellers.Services.GameServices;

public class GamesService : IGameService
{
    private readonly CasinoService _casinoService;
    private readonly MachineRepairService _machineRepairService;
    private readonly WordleGameService _wordleGameService;
    private readonly BossService _bossService;

    public GamesService(
        CasinoService casinoService,
        MachineRepairService machineRepairService,
        WordleGameService wordleGameService, 
        BossService bossService)
    {
        _casinoService = casinoService;
        _machineRepairService = machineRepairService;
        _wordleGameService = wordleGameService;
        _bossService = bossService;
    }
    public GamesResponse GetGameOfTheDay(int userId)
    {
        return DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Monday => _casinoService.GetGameOfTheDay(userId),
            DayOfWeek.Tuesday => _machineRepairService.GetGameOfTheDay(userId),
            DayOfWeek.Wednesday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Thursday => _casinoService.GetGameOfTheDay(userId),
            DayOfWeek.Friday => _machineRepairService.GetGameOfTheDay(userId),
            DayOfWeek.Saturday => throw new AppException("Aucun jeu n'est disponible aujourd'hui.", 400),
            DayOfWeek.Sunday => _bossService.GetGameOfTheDay(userId),
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
            "boss" => _bossService.PlayGameOfTheDay(user, model),
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
            "boss" => _bossService.EstimateGameOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }
    
    public GamesResponse GetWordle(int userId)
        => _wordleGameService.GetGameOfTheDay(userId);
    
    public Task<GamesPlayResponse> PlayWordle(User user, GamesRequest model)
        => _wordleGameService.PlayGameOfTheDay(user, model);

    public async Task<GamesPlayResponse> CancelPlayOfTheDay(User user, GamesRequest model)
    {
        // check if its the game day
        var game = GetGameOfTheDay(user.Id);
        
        if (!string.Equals(game.Name, model.Name, StringComparison.CurrentCultureIgnoreCase))
            throw new AppException("Le jeu demandé n'est pas disponible aujourd'hui.", 400);
        
        return model.Name.ToLower() switch
        {
            "casino" => throw new AppException("Impossible d'annuler une partie de casino.", 400),
            "machine" => throw new AppException("Impossible d'annuler de cette manière.", 400),
            "boss" => await _bossService.CancelPlayOfTheDay(user, model),
            _ => throw new AppException("Le jeu demandé n'existe pas.", 400)
        };
    }
}
