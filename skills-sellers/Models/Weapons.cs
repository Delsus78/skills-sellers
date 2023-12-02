namespace skills_sellers.Models;

public record WeaponResponse(int Id, string Name, string Description);

public record WeaponCreateRequest(string Name, string Description);