using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class UserWeaponsModelsExtensions
{
    public static UserWeaponResponse ToResponse(this UserWeapon userWeapon)
        => new(userWeapon.Id,userWeapon.Weapon.Id, userWeapon.Weapon.Name, userWeapon.Weapon.Description, 
            userWeapon.Power, userWeapon.UserCard?.CardId, userWeapon.Affinity);
    
}