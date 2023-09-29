using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models.Users;

public record UpdateRequest([Required] string Pseudo);

/// <summary>
/// 
/// </summary>
/// <param name="Pseudo"></param>
/// <param name="Password"></param>
/// <param name="ConfirmPassword"></param>
/// <param name="Role"></param>
public record CreateRequest([Required] string Pseudo, [Required] [MinLength(6)] string Password, [Required] string Role)
{
    [Required] [Compare("Password")] 
    public string ConfirmPassword { get; init; }
}

public record UserResponse(int Id, string Pseudo, int NbCards, int Creatium, int Or);