using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models;

public record CardCreateRequest([Required] string Name,
    [Required] string Collection,
    [Required] string Description,
    string Rarity);
public record CardResponse(int Id, string Name, string Collection, string Description, string Rarity);
public record UserCardResponse(int Id,
    string Name,
    string Collection,
    string Description,
    string Rarity,
    CompetencesResponse Competences,
    int Power,
    ActionResponse? Action = null,
    UserWeaponResponse? Weapon = null,
    bool IsDoublon = false,
    bool IsDoublonFullUpgrade = false);