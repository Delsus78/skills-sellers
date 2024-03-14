using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Models;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class SeasonController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }
    
    [HttpGet]
    public SeasonResponse GetActualSeason()
        => _seasonService.GetActualSeason();
    
    [Authorize(Roles = "admin")]
    [HttpPost("end")]
    public SeasonResponse EndSeason()
        => _seasonService.EndSeason();
    
    [Authorize(Roles="admin")]
    [HttpPost("start/{day}")]
    public SeasonResponse StartSeason(int day = 42)
        => _seasonService.StartSeason(day);
}