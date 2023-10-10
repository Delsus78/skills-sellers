using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Users;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet]
    public IEnumerable<UserResponse> GetAll()
        => _userService.GetAll();


    [Authorize]
    [HttpGet("{id}")]
    public UserResponse GetById(int id)
        => _userService.GetById(id);

    [Authorize]
    [HttpGet("{id}/cards")]
    public IEnumerable<UserCardResponse> GetUserCards(int id)
        => _userService.GetUserCards(id);

    [Authorize]
    [HttpGet("{id}/cards/{cardId}")]
    public UserCardResponse GetUserCard(int id, int cardId)
        => _userService.GetUserCard(id, cardId);
    [Authorize]
    [HttpGet("{id}/stats")]
    public StatsResponse GetUserStats(int id)
        => _userService.GetUserStats(id);
    
    [Authorize]
    [HttpGet("{id}/batiments")]
    public UserBatimentResponse GetUserBatiments(int id)
        => _userService.GetUserBatiments(id);

    #region only related user can access to this region

    // helper methods
    private User GetUserAuthenticated()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new AppException("User authenticated not found", 400));
        var user = _userService.GetUserEntity(u => u.Id == userId);
        return user;
    }
    
    [Authorize]
    [HttpPost("{id}/actions")]
    public async Task<ActionResponse> CreateAction(int id, ActionRequest model)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return await _userService.CreateAction(user, model);
    }
    
    [Authorize]
    [HttpPost("{id}/estimate/actions")]
    public ActionEstimationResponse EstimateAction(int id, ActionRequest model)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return _userService.EstimateAction(user, model);
    }
    
    [Authorize]
    [HttpPost("{id}/actions/opencard")]
    public async Task<UserCardResponse?> OpenCard(int id)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id || user.NbCardOpeningAvailable==0)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return await _userService.OpenCard(user);
    }
    
    [Authorize]
    [HttpPost("{id}/cards/{cardId}/ameliorer")]
    public async Task<UserCardResponse> AmeliorerCard(int id, int cardId, CompetencesRequest competencesPointsToGive)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id || user.UserCardsDoubled.All(userCardDb => userCardDb.CardId != cardId))
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return await _userService.AmeliorerCard(user, cardId, competencesPointsToGive);
    }
    
    [Authorize]
    [HttpGet("{id}/notifications")]
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(int id)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        return await _userService.GetNotifications(user);
    }
    
    [Authorize]
    [HttpDelete("{id}/notifications/{notificationId}")]
    public async Task DeleteNotification(int id, int notificationId)
    {
        var user = GetUserAuthenticated();
        if (user.Id != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
        
        await _userService.DeleteNotification(user, notificationId);
    }
    

    #endregion

    #region ADMIN AND AUTH REGION

    [HttpPost("authenticate")]
    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        => await _userService.Authenticate(model);

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _userService.Delete(id);
        return Ok(new { message = "User deleted" });
    }
    
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<UserResponse> Create(UserCreateRequest model)
        => await _userService.Create(model);
    
    [Authorize(Roles = "admin")]
    [HttpPost("{id}/cards/{cardId}")]
    public IActionResult AddCardToUser(int id, int cardId, CompetencesRequest competences)
    {
        _userService.AddCardToUser(id, cardId, competences);
        return Ok(new { message = "Card added to user" });
    }
    
    [Authorize(Roles = "admin")]
    [HttpPost("{id}/batiments")]
    public async Task<UserBatimentResponse> SetLevelOfBatiments(int id, UserBatimentRequest batimentsRequest)
        => await _userService.SetLevelOfBatiments(id, batimentsRequest);

    [Authorize(Roles = "admin")]
    [HttpPost("{id}/actions/forceopencard")]
    public async Task<UserCardResponse?> ForceOpenCard(int id)
        => await _userService.OpenCard(id);
    
    [Authorize(Roles = "admin")]
    [HttpPost("sendnotification")]
    public async Task ForceOpenCard(NotificationRequest notification)
        => await _userService.SendNotificationToAll(notification);

    #endregion
}