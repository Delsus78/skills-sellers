using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface ICosmeticService
{
    List<CosmeticResponse> GetAll();
    CosmeticResponse GetById(int id);
    CosmeticResponse Create(CosmeticCreateRequest model);
    List<UserCosmeticResponse> GetByUserId(int id);
    List<CosmeticResponse> GetTodayUserCosmeticsShop(int id);
    UserCosmeticResponse PlaceCosmetic(int userId, int cosmeticId, CosmeticRequest model);
    UserCosmeticResponse BuyCosmetic(int userId, int cosmeticId, CosmeticRequest model);
}
public class CosmeticService : ICosmeticService
{
    private readonly DataContext _context;

    public CosmeticService(DataContext context)
    {
        _context = context;
    }

    public List<CosmeticResponse> GetAll() 
        => _context.Cosmetics.Select(x => x.ToResponse()).ToList();

    public CosmeticResponse GetById(int id)
    {
        var cosmetic = _context.Cosmetics.FirstOrDefault(c => c.Id == id);
        if (cosmetic == null)
            throw new AppException("Cosmetic not found", 404);

        return cosmetic.ToResponse();
    }

    public CosmeticResponse Create(CosmeticCreateRequest model)
    {
        var cosmetic = model.ToEntity();

        _context.Cosmetics.Add(cosmetic);
        _context.SaveChanges();

        return cosmetic.ToResponse();
    }

    public List<UserCosmeticResponse> GetByUserId(int id)
    {
        var cosmetics = _context.UserCosmetics.Where(c => c.UserId == id)
            .Include(uc => uc.Cosmetic)
            .ToList();
        if (cosmetics == null)
            throw new AppException("Cosmetics not found", 404);

        return cosmetics.Select(c => c.ToResponse()).ToList();
    }

    public List<CosmeticResponse> GetTodayUserCosmeticsShop(int id)
    {
        // seed cosmetics
        var cosmetics = _context.Cosmetics.ToList();
        
        // get 4 random cosmetics based on their rarity
        var seed = id + DateTime.Now.DayOfYear + DateTime.Now.Year;
        var random = new Random(seed);
        
        var randomCosmetics = new List<Cosmetic>();
        for (var i = 0; i < 4; i++)
        {
            // rarity is 1 commun, 2 rare, 3 legendary
            // commun = 70%, rare = 28%, legendary = 2%
            var rarity = random.Next(1, 101) switch { <= 70 => 1, <= 98 => 2, _ => 3 };

            var rarityCosmetics = cosmetics.Where(c => c.Rarity == rarity).ToList();
            
            var randomCosmetic = rarityCosmetics.Count == 0 
                ? cosmetics[random.Next(0, cosmetics.Count)] 
                : rarityCosmetics[random.Next(0, rarityCosmetics.Count)];
            
            randomCosmetics.Add(randomCosmetic);
            cosmetics.Remove(randomCosmetic);
        }
        
        return randomCosmetics.Select(c => c.ToResponse()).ToList();
        
    }

    public UserCosmeticResponse PlaceCosmetic(int userId, int cosmeticId, CosmeticRequest model)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            throw new AppException("User not found", 404);

        var userCosmetic = _context.UserCosmetics
            .Where(c => c.UserId == userId && c.Id == cosmeticId)
            .Include(c => c.Cosmetic)
            .FirstOrDefault();
        if (userCosmetic == null)
            throw new AppException("Cosmetic not found", 404);

        userCosmetic.CoordinateX = model.CoordinateX;
        userCosmetic.CoordinateY = model.CoordinateY;
        userCosmetic.Size = model.Size;
        
        _context.SaveChanges();
        
        return userCosmetic.ToResponse();
    }

    public UserCosmeticResponse BuyCosmetic(int userId, int cosmeticId, CosmeticRequest model)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            throw new AppException("User not found", 404);

        var buyableCosmetic = GetTodayUserCosmeticsShop(userId);
        
        var cosmetic = buyableCosmetic.FirstOrDefault(c => c.Id == cosmeticId);
        if (cosmetic == null)
            throw new AppException("Cosmetic not found", 404);

        if (user.CosmeticPoints() < cosmetic.Price)
            throw new AppException("Pas assez de points de cosmétique.", 400);
        
        user.SpendedCosmeticPoints += cosmetic.Price;
        
        var userCosmetic = new UserCosmetic
        {
            UserId = userId,
            CosmeticId = cosmeticId,
            CoordinateX = model.CoordinateX,
            CoordinateY = model.CoordinateY,
            Size = model.Size
        };
        
        _context.UserCosmetics.Add(userCosmetic);
        
        _context.SaveChanges();
        
        return userCosmetic.ToResponse();
    }
}