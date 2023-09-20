using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models.Users;

public record UpdateRequest([Required] string Pseudo);

public record CreateRequest([Required] string Pseudo)
{
    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
    
    [Required]
    /**
     * Allowed values: "user", "admin"
     */
    public string Role { get; set; }
}