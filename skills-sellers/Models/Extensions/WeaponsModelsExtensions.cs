using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class WeaponsModelsExtensions
{
    public static WeaponResponse ToResponse(this Weapon weapon)
        => new(weapon.Id, weapon.Name, weapon.Description);
    
    public static Weapon CreateWeapon(this WeaponCreateRequest model)
        => new()
        {
            Name = model.Name,
            Description = model.Description
        };
}