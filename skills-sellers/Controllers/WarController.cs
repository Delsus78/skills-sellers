using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WarController : ControllerBase
{
    private readonly IWarService _warService;
    private readonly IUserService _userService;

    public WarController(IWarService warService, IUserService userService)
    {
        _warService = warService;
        _userService = userService;
    }
    
    // helper methods
    private User GetUserAuthenticated()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new AppException("User authenticated not found", 400));
        var user = _userService.GetUserEntity(u => u.Id == userId);
        return user;
    }
    
    [HttpPost("start")]
    public async Task StartWar(WarCreationRequest model)
        => await _warService.StartWar(GetUserAuthenticated(), model);
    
    [HttpPost("cancel/{warId}")]
    public async Task CancelWar(int warId)
        => await _warService.CancelWar(GetUserAuthenticated(), warId);

    [HttpPost("accept/{warId}")]
    public async Task<WarResponse> AcceptWar(int warId, AddCardsToWarRequest model)
        => await _warService.AcceptWar(GetUserAuthenticated(), warId, model);
    
    [HttpPost("decline/{warId}")]
    public async Task DeclineWar(int warId)
        => await _warService.DeclineWar(GetUserAuthenticated(), warId);
    
    [HttpPost("estimate")]
    public async Task<WarEstimationResponse> EstimateWar(WarCreationRequest model)
        => await _warService.EstimateWar(GetUserAuthenticated(), model);
    
    [HttpGet("invitedWar")]
    public async Task<WarResponse?> GetInvitedWar()
        => await _warService.GetInvitedWar(GetUserAuthenticated());
    
    [HttpGet("warLoot/estimate")]
    public WarLootEstimationResponse GetEstimatedWarLootForUser(int multiplicator = 1, int reducedPourcentage = 0)
        => _warService.GetEstimatedWarLootForUser(multiplicator, reducedPourcentage);
    
    [HttpPost("simulate")]
    public WarSimulationResponse SimulateWar(WarSimulationRequest model)
        => _warService.SimulateWar(model);

    [Authorize(Roles = "admin")]
    [HttpPost("warLoot/{userId}")]
    public async Task GiveRandomWarLoot(int userId, int multiplicator = 1)
        => await _warService.GiveRandomWarLoot(userId, multiplicator);
}