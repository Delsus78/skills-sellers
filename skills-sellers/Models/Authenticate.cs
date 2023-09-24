using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models;

public record AuthenticateRequest([Required] string Pseudo, [Required] string Password);

public record AuthenticateResponse(int Id, string Pseudo, string Token);