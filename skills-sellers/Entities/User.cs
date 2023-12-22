using System.ComponentModel.DataAnnotations.Schema;
using skills_sellers.Entities.Registres;
using skills_sellers.Entities.Speciales;

namespace skills_sellers.Entities;

[Table("users")]
public class User
{
    public int Id { get; set; }
    public string Pseudo { get; set; }

    // Many to many
    public List<UserCard> UserCards { get; } = new();
    public List<UserCardDoubled> UserCardsDoubled { get; } = new();
    public List<UserWeapon> UserWeapons { get; } = new();
    public List<Notification> Notifications { get; } = new();
    public List<Registre> Registres { get; set; } = new();
    public int Score { get; set; }
    public int Creatium { get; set; }
    public int Or { get; set; }
    public int Nourriture { get; set; }
    public int NbCardOpeningAvailable { get; set; }
    public int NbWeaponOpeningAvailable { get; set; }
    public int NbWeaponUpgradeAvailable { get; set; }

    // One to one
    public Stats Stats { get; set; }
    public Achievement Achievement { get; set; }
    public UserBatimentData UserBatimentData { get; set; }
    public WordleGame WordleGame { get; set; }
    public UserRegistreInfo UserRegistreInfo { get; set; }
    
    // specials
    public Christmas Christmas { get; set; }
}