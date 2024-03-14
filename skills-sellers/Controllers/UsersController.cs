using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Models;
using skills_sellers.Models.Users;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

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
    [HttpGet("{id}/weapons")]
    public IEnumerable<UserWeaponResponse> GetUserWeapons(int id)
        => _userService.GetUserWeapons(id);

    [Authorize]
    [HttpGet("{id}/weapons/{weaponId}")]
    public UserWeaponResponse GetUserWeapons(int id, int weaponId)
        => _userService.GetUserWeapon(id, weaponId);
    
    [Authorize]
    [HttpGet("{id}/stats")]
    public StatsResponse GetUserStats(int id)
        => _userService.GetUserStats(id);

    [Authorize]
    [HttpGet("{id}/batiments")]
    public UserBatimentResponse GetUserBatiments(int id)
        => _userService.GetUserBatiments(id);

    #region ADMIN AND AUTH REGION

    [HttpPost("authenticate")]
    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
        var res = await _userService.Authenticate(model);
        // set cookies
        Response.Headers.Add("Set-Cookie", $"user_token=${res.Token}; Path=/; SameSite=None;");
        
        return res;
    }

    [HttpPost("resetpassword")]
    public async Task<AuthenticateResponse> ResetPassword(ResetPasswordRequest model)
        => await _userService.ResetPassword(model);
    
    [HttpPost("register")]
    public Task<UserResponse> Register(UserRegisterRequest model)
        => _userService.Register(model);

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
    [HttpPut("createLink")]
    public RegistrationLinkResponse CreateLink(LinkCreateRequest model)
        => _userService.CreateLink(model);

    [Authorize(Roles = "admin")]
    [HttpPut("createresetpasswordlink")]
    public Task<ResetPasswordLinkResponse> CreateResetPasswordLink(ResetPasswordLinkRequest model)
        => _userService.CreateResetPasswordLink(model);

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
    public async Task SendNotification(NotificationRequest notification)
        => await _userService.SendNotificationToAll(notification);

    [Authorize(Roles = "admin")]
    [HttpPost("createGiftCode")]
    public async Task<GiftCodeResponse> CreateGiftCode(GiftCodeCreationRequest giftCodeCreation)
        => await _userService.CreateGiftCode(giftCodeCreation);
    
    #endregion
}