using skills_sellers.Entities;

namespace skills_sellers.Models;

public record WeaponResponse(int Id, string Name, string Description);

public record WeaponCreateRequest(string Name, string Description);

public record UserWeaponResponse(
    int Id, 
    string Name, 
    string Description, 
    int Power, 
    int? UserCardId,
    WeaponAffinity Affinity);