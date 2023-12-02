using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Models;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WeaponsController : ControllerBase
{
    private IWeaponService _weaponService;

    public WeaponsController(
        IWeaponService weaponService) 
        => _weaponService = weaponService;

    [Authorize(Roles = "admin")]
    [HttpGet]
    public IEnumerable<WeaponResponse> GetAll()
        => _weaponService.GetAll();
    
    [HttpGet("{id}")]
    public WeaponResponse GetById(int id)
        => _weaponService.GetById(id);

    [Authorize(Roles = "admin")]
    [HttpPost]
    public WeaponResponse Create(WeaponCreateRequest model)
        => _weaponService.Create(model);
}