using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class CosmeticsModelsExtensions
{
    public static UserCosmeticResponse ToResponse(this UserCosmetic userCosmetic)
    {
        return new UserCosmeticResponse(
            userCosmetic.Id,
            userCosmetic.Cosmetic.Name,
            userCosmetic.Cosmetic.Id,
            userCosmetic.Cosmetic.Rarity,
            userCosmetic.CoordinateX,
            userCosmetic.CoordinateY,
            userCosmetic.Size);
    }

    public static CosmeticResponse ToResponse(this Cosmetic cosmetic)
    {
        return new CosmeticResponse(
            cosmetic.Id,
            cosmetic.Name,
            cosmetic.Price,
            cosmetic.Rarity);
    }
    
    public static Cosmetic ToEntity(this CosmeticCreateRequest model)
    {
        return new Cosmetic
        {
            Name = model.Name,
            Price = model.Price,
            Rarity = model.Rarity
        };
    }
}