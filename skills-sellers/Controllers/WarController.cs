using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WarController : ControllerBase
{
    
}