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
[Route("Users/{id}")]
public class UserActionController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMarchandService _marchandService;
    private readonly IAchievementsService _achievementsService;
    private readonly IWeaponService _weaponService;

    public UserActionController(
        IUserService userService, IMarchandService marchandService, IAchievementsService achievementsService, IWeaponService weaponService)
    {
        _userService = userService;
        _marchandService = marchandService;
        _achievementsService = achievementsService;
        _weaponService = weaponService;
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
    public async Task<UserCardResponse> OpenCard(int id)
        => await _userService.OpenCard(GetUserAuthenticated(id));
    
    [Authorize]
    [HttpPost("actions/openweapon")]
    public async Task<UserWeaponResponse?> OpenWeapon(int id)
        => await _weaponService.OpenWeapon(GetUserAuthenticated(id));
    
    [Authorize]
    [HttpPost("cards/{cardId}/applyweapon/{userWeaponId}")]
    public async Task<UserCardResponse?> ApplyWeaponToUserCard(int id, int cardId, int? userWeaponId)
        => await _weaponService.ApplyWeaponToUserCard(GetUserAuthenticated(id), cardId, userWeaponId);
    
    [Authorize]
    [HttpPost("cards/{cardId}/ameliorer")]
    public async Task<UserCardResponse> AmeliorerCard(int id, int cardId, CompetencesRequest competencesPointsToGive)
    {
        var user = GetUserAuthenticated(id);
        
        var referer = Request.Headers["Referer"].ToString();
        if (!referer.Contains("localhost:5173") && !referer.Contains("skills-sellers.fr"))
            await _userService.ResponseToBottedAgent(user);
        
        return await _userService.AmeliorerCard(user, cardId, competencesPointsToGive);
    }

    [Authorize]
    [HttpPost("weapons/{weaponId}/ameliorer")]
    public async Task<UserWeaponResponse> AmeliorerWeapon(int id, int weaponId)
        => await _userService.AmeliorerWeapon(GetUserAuthenticated(id), weaponId);

    [Authorize]
    [HttpPost("actions/decision")]
    public async Task<ActionResponse> PostDecisionForAction(int id, ActionDecisionRequest model) 
        => await _userService.DecideForAction(GetUserAuthenticated(id), model);

    [Authorize]
    [HttpPost("actions")]
    public async Task<List<ActionResponse>> CreateAction(int id, ActionRequest model)
    {
        var user = GetUserAuthenticated(id);
        
        var referer = Request.Headers["Referer"].ToString();
        if (!referer.Contains("localhost:5173") && !referer.Contains("skills-sellers.fr"))
            await _userService.ResponseToBottedAgent(user);

        return await _userService.CreateAction(user, model);
    }


    [Authorize]
    [HttpPost("estimate/actions")]
    public ActionEstimationResponse EstimateAction(int id, ActionRequest model)
        => _userService.EstimateAction(GetUserAuthenticated(id), model);
    
    [Authorize]
    [HttpPost("satellite/switchAutoMode/{actionId}")]
    public void SwitchAutoSatelliteMode(int id, int actionId)
        => _userService.SwitchAutoSatelliteMode(GetUserAuthenticated(id), actionId);
    
    [Authorize]
    [HttpDelete("actions/{actionId}")]
    public async Task DeleteAction(int id, int actionId)
        => await _userService.DeleteAction(GetUserAuthenticated(id), actionId);

    [Authorize]
    [HttpGet("cards/{cardId}")]
    public UserCardResponse GetUserCard(int id, int cardId)
        => _userService.GetUserCard(GetUserAuthenticated(id), cardId);
    

    [Authorize]
    [HttpGet("notifications")]
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(int id)
        => await _userService.GetNotifications(GetUserAuthenticated(id));

    [Authorize]
    [HttpPost("notification/{userId}")]
    public async Task SendNotification(int id, int userId, NotificationRequest notification)
        => await _userService.SendNotification(GetUserAuthenticated(id), userId, notification);
    
    [Authorize]
    [HttpDelete("notifications")]
    public async Task DeleteNotifications(int id, List<int> notificationIds)
        => await _userService.DeleteNotifications(GetUserAuthenticated(id), notificationIds);
    
    [Authorize]
    [HttpPost("marchand/buy")]
    public async Task BuyMarchandOffer(int id)
        => await _marchandService.BuyMarchandAsync(GetUserAuthenticated(id));
    
    [Authorize]
    [HttpGet("marchand/offer")]
    public MarchandShopResponse GetMarchandOffer()
        => _marchandService.GetMarchandShop();
    
    [Authorize]
    [HttpPost("gift")]
    public Task<GiftCodeResponse> EnterGiftCode(int id, GiftCodeRequest giftCode)
        => _userService.EnterGiftCode(GetUserAuthenticated(id), giftCode);
    
    [Authorize]
    [HttpGet("achievements")]
    public async Task<AchievementResponse> GetAchievements(int id)
        => await _achievementsService.GetAll(id);
    
    [Authorize]
    [HttpPost("achievements")]
    public async Task<AchievementResponse?> ClaimAchievement(int id, AchievementRequest achievement)
        => await _achievementsService.ClaimAchievement(GetUserAuthenticated(id), achievement);
}