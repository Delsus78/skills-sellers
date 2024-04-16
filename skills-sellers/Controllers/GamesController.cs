using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using skills_sellers.Services;
using skills_sellers.Services.GameServices;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]/{id:int}")]
public class  GamesController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly GamesService _gamesService;
    private readonly IChristmasService _christmasService;

    public GamesController(
        IUserService userService,
        GamesService gamesService,
        IChristmasService christmasService)
    {
        _userService = userService;
        _gamesService = gamesService;
        _christmasService = christmasService;
    }

    // helper methods
    private User GetUserAuthenticated(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new AppException("User authenticated not found", 400));
        var user = _userService.GetUserEntity(u => u.Id == userId);
        
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return user;
    }
    
    [HttpGet("gameOfTheDay")]
    public GamesResponse GetGameOfTheDay(int id)
        => _gamesService.GetGameOfTheDay(id);
    
    [HttpPost("gameOfTheDay/estimate")]
    public GamesPlayResponse EstimateGameOfTheDay(int id, GamesRequest model)
        => _gamesService.EstimateGameOfTheDay(GetUserAuthenticated(id), model);
    
    [HttpPost("gameOfTheDay/play")]
    public async Task<GamesPlayResponse> PlayGameOfTheDay(int id, GamesRequest model)
        => await _gamesService.PlayGameOfTheDay(GetUserAuthenticated(id), model);

    [HttpPost("gameOfTheDay/cancel")]
    public async Task<GamesPlayResponse> CancelPlayOfTheDay(int id, GamesRequest model)
        => await _gamesService.CancelPlayOfTheDay(GetUserAuthenticated(id), model);
    
    [HttpGet("wordle")]
    public GamesResponse GetWordle(int id)
        => _gamesService.GetWordle(id);
    
    [HttpPost("wordle")]
    public async Task<GamesPlayResponse> PlayWordle(int id, GamesRequest model)
        => await _gamesService.PlayWordle(GetUserAuthenticated(id), model);
    
    [HttpPost("blackjack")]
    public GamesPlayResponse PlayBlackJack(int id, BlackJackAction action)
        => _gamesService.PlayBlackJack(GetUserAuthenticated(id), action);

    [HttpGet("christmas")]
    public async Task<ChristmasResponse> GetChristmas(int id)
        => (await _christmasService.GetDaysGiftOpened(GetUserAuthenticated(id))).ToResponse();
    
    [HttpPost("christmas")]
    public async Task<ChristmasResponse> OpenChristmas(int id) 
        => (await _christmasService.OpenDayGift(GetUserAuthenticated(id))).ToResponse();
}