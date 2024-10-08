using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using skills_sellers.Helpers;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IMemoryCache _cache;

    public ImagesController(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    [HttpGet("{id}")]
    public IActionResult GetImage(int id)
    {
        var imagePath = Path.Combine("Images", id + ".jpg");
        if (_cache.TryGetValue(imagePath, out byte[]? imageBytes))
            if (imageBytes != null)
                return File(imageBytes, "image/jpeg"); // Assume all images are JPEGs

        if (System.IO.File.Exists(imagePath))
        {
            imageBytes = System.IO.File.ReadAllBytes(imagePath);
            _cache.Set(imagePath, imageBytes, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // cache for 1 hour
            });
        } else return NotFound();
        
        
        Response.Headers["Cache-Control"] = "public,max-age=604800"; // cache for 1 week

        return File(imageBytes, "image/jpeg");
    }
    
    [HttpGet("weapon/{id}")]
    public IActionResult GetWeaponImage(int id)
    {
        var imagePath = Path.Combine("Images/Weapons", id + ".jpg");
        if (_cache.TryGetValue(imagePath, out byte[]? imageBytes))
            if (imageBytes != null)
                return File(imageBytes, "image/jpeg"); // Assume all images are JPEGs
        
        if (System.IO.File.Exists(imagePath))
        {
            imageBytes = System.IO.File.ReadAllBytes(imagePath);
            _cache.Set(imagePath, imageBytes, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // cache for 1 hour
            });
        } else return NotFound();
        
        
        Response.Headers["Cache-Control"] = "public,max-age=604800"; // cache for 1 week

        return File(imageBytes, "image/jpeg");
    }
}