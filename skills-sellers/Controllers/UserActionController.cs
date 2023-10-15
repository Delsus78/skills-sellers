using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Models.Cards;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("Users/{id}")]
public class UserActionController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMarchandService _marchandService;

    public UserActionController(
        IUserService userService, IMarchandService marchandService)
    {
        _userService = userService;
        _marchandService = marchandService;
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
    
    [Authorize]
    [HttpPost("actions/opencard")]
    public async Task<UserCardResponse?> OpenCard(int id)
        => await _userService.OpenCard(GetUserAuthenticated(id));
    
    [Authorize]
    [HttpPost("cards/{cardId}/ameliorer")]
    public async Task<UserCardResponse> AmeliorerCard(int id, int cardId, CompetencesRequest competencesPointsToGive)
        => await _userService.AmeliorerCard(GetUserAuthenticated(id), cardId, competencesPointsToGive);

    [Authorize]
    [HttpPost("actions")]
    public async Task<ActionResponse> CreateAction(int id, ActionRequest model)
        => await _userService.CreateAction(GetUserAuthenticated(id), model);
    
    
    [Authorize]
    [HttpPost("estimate/actions")]
    public ActionEstimationResponse EstimateAction(int id, ActionRequest model)
        => _userService.EstimateAction(GetUserAuthenticated(id), model);

    [Authorize]
    [HttpGet("cards/{cardId}")]
    public UserCardResponse GetUserCard(int id, int cardId)
        => _userService.GetUserCard(GetUserAuthenticated(id), cardId);
    

    [Authorize]
    [HttpGet("notifications")]
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(int id)
        => await _userService.GetNotifications(GetUserAuthenticated(id));
    
    
    [Authorize]
    [HttpDelete("notifications/{notificationId}")]
    public async Task DeleteNotification(int id, int notificationId)
        => await _userService.DeleteNotification(GetUserAuthenticated(id), notificationId);
    
    [Authorize]
    [HttpPost("marchand/buy")]
    public async Task BuyMarchandOffer(int id)
        => await _marchandService.BuyMarchandAsync(GetUserAuthenticated(id));
    
    [Authorize]
    [HttpGet("marchand/offer")]
    public MarchandShopResponse GetMarchandOffer()
        => _marchandService.GetMarchandShop();
}