using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Users;
using skills_sellers.Services;
using CreateRequest = skills_sellers.Models.Users.CreateRequest;

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
    public IActionResult Create(CreateRequest model)
    {
        _userService.Create(model);
        return Ok(new { message = "User created" });
    }
    
    [Authorize(Roles = "admin")]
    [HttpPost("{id}/cards/{cardId}")]
    public IActionResult AddCardToUser(int id, int cardId, CompetencesRequest competences)
    {
        _userService.AddCardToUser(id, cardId, competences);
        return Ok(new { message = "Card added to user" });
    }
    
    // helper methods

    private User GetUserAuthenticated()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new AppException("User authenticated not found", 400));
        var user = _userService.GetUserEntity(u => u.Id == userId);
        return user;
    }
}