using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
        byte[] imageBytes;
        
        if (_cache.TryGetValue(id, out imageBytes))
            return File(imageBytes, "image/jpeg"); // Assume all images are JPEGs
        
        var imagePath = Path.Combine("Images", id + ".jpg");
        if (System.IO.File.Exists(imagePath))
        {
            imageBytes = System.IO.File.ReadAllBytes(imagePath);
            _cache.Set(id, imageBytes, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // cache for 1 hour
            });
        } else return NotFound();

        return File(imageBytes, "image/jpeg"); // Assume all images are JPEGs
    }

}