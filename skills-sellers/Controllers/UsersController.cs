using System.Security.Claims;
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
    private IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _userService.GetAll();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var user = _userService.GetById(id);
        return Ok(user);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public IActionResult Create(CreateRequest model)
    {
        // get current user id
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _userService.Create(model);
        return Ok(new { message = "User created" });
    } 
    
    [HttpPost("authenticate")]
    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        => await _userService.Authenticate(model);

/*
    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateRequest model)
    {
        _userService.Update(id, model);
        return Ok(new { message = "User updated" });
    }
*/

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _userService.Delete(id);
        return Ok(new { message = "User deleted" });
    }
    
    [HttpPost("{id}/cards/{cardId}")]
    public IActionResult AddCardToUser(int id, int cardId)
    {
        _userService.AddCardToUser(id, cardId);
        return Ok(new { message = "Card added to user" });
    }
}