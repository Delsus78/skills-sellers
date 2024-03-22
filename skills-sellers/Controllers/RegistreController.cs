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
[Route("[controller]/{id:int}")]
public class RegistreController : ControllerBase
{
    private readonly IRegistreService _registreService;
    private readonly IUserService _userService;

    public RegistreController(IRegistreService registreService, IUserService userService)
    {
        _registreService = registreService;
        _userService = userService;
    }
    
    private User GetUserAuthenticated(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new AppException("User authenticated not found", 400));
        var user = _userService.GetUserEntity(u => u.Id == userId);
        
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return user;
    }
    
    [Authorize]
    [HttpDelete("friendly/{registreId}")]
    public async Task DeleteFriendlyRegistre(int id, int registreId) 
        => await _registreService.DeleteFriendlyRegistre(GetUserAuthenticated(id), registreId);
    
        
    [Authorize]
    [HttpGet("registreInfo")]
    public UserRegistreInfoResponse GetRegistreInfo(int id)
        => _userService.GetRegistreInfo(id);
    
    [Authorize]
    [HttpGet("fightreports")]
    public IEnumerable<FightReportResponse> GetFightReports(int limit = 20)
        => _registreService.GetFightReports(limit);
    
    [Authorize]
    [HttpPost("favorite/{registreId}")]
    public async Task SwitchFavorite(int id, int registreId)
        => await _registreService.SwitchFavorite(GetUserAuthenticated(id), registreId);
}