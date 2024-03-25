using System.Linq.Expressions;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface IWeaponService
{
    IEnumerable<WeaponResponse> GetAll();
    int GetCount();
    WeaponResponse GetById(int id);
    WeaponResponse Create(WeaponCreateRequest model);
    Weapon GetWeaponEntity(Expression<Func<Weapon, bool>> predicate);
    Weapon GetRandomWeapon();
    Task<UserWeaponResponse?> OpenWeapon(User user);
    Task<UserCardResponse?> ApplyWeaponToUserCard(User user, int cardId, int? userWeaponId);
    
    (int creatiumPrice, int orPrice) GetWeaponConstructionPrice(int numberOfCards, int numberOfWeapons);
    (int creatiumPrice, int intelPrice, int forcePrice) GetWeaponPrices(int actualPower, int numberOfWeapons, int numberOfCards);
}
public class WeaponService : IWeaponService
{
    private readonly DataContext _context;

    public WeaponService(DataContext context)
    {
        _context = context;
    }

    public IEnumerable<WeaponResponse> GetAll()
        => _context.Weapons.Select(x => x.ToResponse());

    public int GetCount()
        => _context.Weapons.Count();

    public WeaponResponse GetById(int id)
        => GetWeaponEntity(c => c.Id == id).ToResponse();

    public WeaponResponse Create(WeaponCreateRequest model)
    {
        // validate
        if (_context.Weapons.Any(x => x.Name == model.Name))
            throw new AppException("Weapon with the name '" + model.Name + "' already exists", 400);

        // map model to new user object
        var weapon = model.CreateWeapon();

        // save user
        _context.Weapons.Add(weapon);
        _context.SaveChanges();

        return weapon.ToResponse();
    }

    public Weapon GetWeaponEntity(Expression<Func<Weapon, bool>> predicate)
    {
        var weapon = _context.Weapons.FirstOrDefault(predicate);

        if (weapon == null)
            throw new AppException("Weapon not found", 404);

        return weapon;
    }

    
    public async Task<UserWeaponResponse?> OpenWeapon(User user)
    {
        // 0 weapon available
        if (user.NbWeaponOpeningAvailable <= 0)
            throw new AppException("Vous n'avez plus d'arme disponible !", 400);
        
        // remove weapon opening
        user.NbWeaponOpeningAvailable--;
        
        // get random weapon
        var weapon = GetRandomWeapon();
        
        // get random affinity
        var affinity = Randomizer.RandomWeaponAffinity();
        
        // get random start power
        var power = Randomizer.GetRandomExploRarityNumber();
        
        // add weapon to user
        var userWeapon = new UserWeapon
        {
            User = user,
            Weapon = weapon,
            Affinity = affinity,
            Power = power
        };
        
        _context.UserWeapons.Add(userWeapon);
        
        // save changes
        await _context.SaveChangesAsync();
        
        return userWeapon.ToResponse();
    }

    public async Task<UserCardResponse?> ApplyWeaponToUserCard(User user, int cardId, int? userWeaponId)
    {
        // get user card
        var userCard = user.UserCards.FirstOrDefault(uc => uc.CardId == cardId);
        
        if (userCard == null)
            throw new AppException("Carte non trouvée", 404);
        
        if (userCard.Action != null)
            throw new AppException("Vous ne pouvez pas équiper une carte en action", 400);
        
        // weapon in labo ?
        if (userWeaponId != null && user.UserCards.Any(card => card.Action is ActionAmeliorer ameliorer && ameliorer.WeaponToUpgradeId == userWeaponId))
            throw new AppException("Cette arme est en amélioration", 400);

        // get user weapon, if not found, desequipe weapon
        var userWeapon = user.UserWeapons.FirstOrDefault(uw => uw.Id == userWeaponId);
        
        userCard.UserWeapon = userWeapon;
        
        // save changes
        await _context.SaveChangesAsync();
        
        return userCard.ToResponse();
    }

    public Weapon GetRandomWeapon()
    {
        var weaponCount = _context.Weapons.Count();
        var randomIndex = Randomizer.RandomInt(0, weaponCount);
        Console.Out.WriteLine($"Random weapon index : {randomIndex}");

        // Récupérez seulement la carte sélectionnée
        return _context.Weapons.Skip(randomIndex).First();
    }

    public (int creatiumPrice, int orPrice) GetWeaponConstructionPrice(int numberOfCards, int numberOfWeapons)
    {
        var creatiumPrice = (int)(1500 * (Math.Pow(5, numberOfCards / 100.0) + numberOfWeapons * 4));
        var orPrice = (int)(500 * (Math.Pow(5, numberOfCards / 100.0) + numberOfWeapons * 2));
        
        return (creatiumPrice, orPrice);
    }

    public (int creatiumPrice, int intelPrice, int forcePrice) GetWeaponPrices(int actualPower, int numberOfWeapons, int numberOfCards)
    {
        var creatiumPrice = (int)(150 * (Math.Pow(6, numberOfCards / 100.0) + numberOfWeapons * 4) * Math.Pow(1.4,actualPower));
        var intelPrice = actualPower * 25;
        var forcePrice = actualPower * 50;
        
        return (creatiumPrice, intelPrice, forcePrice);
    }
}
