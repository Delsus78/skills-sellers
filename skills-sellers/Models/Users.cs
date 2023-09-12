using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models;

public record UpdateRequest([Required] string Pseudo);

public record CreateRequest([Required] string Pseudo)
{
/*
    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
*/
}