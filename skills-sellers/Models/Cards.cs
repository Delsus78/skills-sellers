using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models.Cards;

public record CreateRequest([Required] string Name, [Required] string ImageUrl, [Required] string Description, string Rarity);
public record CardResponse(int Id, string Name, string ImageUrl, string Description, string Rarity);
public record UserCardResponse(int Id, string Name, string ImageUrl, string Description, string Rarity, CompetencesResponse Competences);