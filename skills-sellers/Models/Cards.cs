using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models.Cards;

public record CreateRequest([Required] string Name, [Required] string ImageUrl, [Required] string Description);
public record CardResponse(int Id, string Name, string ImageUrl, string Description);
public record UserCardResponse(int Id, string Name, string ImageUrl, string Description, CompetencesResponse Competences);