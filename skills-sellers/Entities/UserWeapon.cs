namespace skills_sellers.Entities;

public class UserWeapon
{
    public int Id { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
    public Weapon Weapon { get; set; }
    public int WeaponId { get; set; }
    
    public UserCard UserCard { get; set; }
    
    public int Power { get; set; }
    public WeaponAffinity Affinity { get; set; }
}


public enum WeaponAffinity
{
    Pierre,
    Feuille,
    Ciseaux
}