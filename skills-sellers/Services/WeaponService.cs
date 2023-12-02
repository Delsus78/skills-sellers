using System.Linq.Expressions;
using skills_sellers.Entities;
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
    
    (int creatiumPrice, int orPrice) GetWeaponConstructionPrice(int numberOfCards);
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

    public Weapon GetRandomWeapon()
    {
        var weaponCount = _context.Weapons.Count();
        var randomIndex = Randomizer.RandomInt(0, weaponCount);
        Console.Out.WriteLine($"Random weapon index : {randomIndex}");

        // Récupérez seulement la carte sélectionnée
        return _context.Weapons.Skip(randomIndex).First();
    }

    public (int creatiumPrice, int orPrice) GetWeaponConstructionPrice(int numberOfCards)
    {
        var creatiumPrice = (int)(5000 * Math.Pow(10, numberOfCards / 100.0));
        var orPrice = (int)(1000 * Math.Pow(10, numberOfCards / 100.0));
        
        return (creatiumPrice, orPrice);
    }
}