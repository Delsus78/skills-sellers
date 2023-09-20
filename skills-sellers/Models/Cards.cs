using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models.Cards;

public record CreateRequest([Required] string Name, [Required] string ImageUrl, [Required] string Description);
public record UpdateRequest([Required] string Name, [Required] string ImageUrl, [Required] string Description);
