using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Helpers;
using skills_sellers.Models;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class CosmeticController : ControllerBase
{
    private readonly ICosmeticService _cosmeticService;

    public CosmeticController(ICosmeticService cosmeticService)
    {
        _cosmeticService = cosmeticService;
    }
    
    private void IsUserAuthenticated(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) 
                               ?? throw new AppException("User authenticated not found", 400));
        if (userId != id)
            throw new AppException("Vous n'êtes pas autorisé à effectuer cette action.", 400);
    }
    
    [HttpGet]
    public IEnumerable<CosmeticResponse> GetAll()
        => _cosmeticService.GetAll();
    
    [HttpGet("{id}")]
    public CosmeticResponse GetById(int id)
        => _cosmeticService.GetById(id);
    
    [HttpGet("user/{id}")]
    public IEnumerable<UserCosmeticResponse> GetByUserId(int id)
        => _cosmeticService.GetByUserId(id);
    
    [HttpGet("shop/{id}")]
    public IEnumerable<CosmeticResponse> GetTodayUserCosmeticsShop(int id)
        => _cosmeticService.GetTodayUserCosmeticsShop(id);
    
    [HttpPost("place/{userId}/{cosmeticId}")]
    public UserCosmeticResponse PlaceCosmetic(int userId, int cosmeticId, CosmeticRequest model)
    {
        IsUserAuthenticated(userId);
        return _cosmeticService.PlaceCosmetic(userId, cosmeticId, model);
    }

    [HttpPost("buy/{userId}/{cosmeticId}")]
    public UserCosmeticResponse BuyCosmetic(int userId, int cosmeticId, CosmeticRequest model)
    {
        IsUserAuthenticated(userId);
        return _cosmeticService.BuyCosmetic(userId, cosmeticId, model);
    }
    

    [Authorize(Roles = "admin")]
    [HttpPost]
    public CosmeticResponse Create(CosmeticCreateRequest model)
        => _cosmeticService.Create(model);
}