namespace skills_sellers.Entities;

public class UserWeapon
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    
    public int WeaponId { get; set; }
    public Weapon Weapon { get; set; }
    
    // Carte associée à cette combinaison User-Weapon
    public UserCard UserCard { get; set; }
    
    // Compétences associées à cette combinaison User-Card
    public int Power { get; set; }
    
    public WeaponAffinity Affinity { get; set; }
}


public enum WeaponAffinity
{
    Pierre,
    Feuille,
    Ciseaux
}