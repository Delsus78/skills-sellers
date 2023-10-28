using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models;

public record AuthenticateRequest([Required] string Pseudo, [Required] string Password);

public record ResetPasswordRequest([Required] [MinLength(6)] string Password, string link)
{
    [Required] [Compare("Password")] 
    public string ConfirmPassword { get; init; }
}

public record AuthenticateResponse(int Id, string Pseudo, string Token);
