using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]/{id:int}")]
public class RegistreController : ControllerBase
{
    private readonly IRegistreService _registreService;

    public RegistreController(IRegistreService registreService)
    {
        _registreService = registreService;
    }
}