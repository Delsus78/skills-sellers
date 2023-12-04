namespace skills_sellers.Entities;

public class UserCard
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int CardId { get; set; }
    public Card Card { get; set; }

    // Compétences associées à cette combinaison User-Card
    public Competences Competences { get; set; }
    
    // Action associée à cette combinaison User-Card
    public Action? Action { get; set; }
    
    // Weapon
    public int? UserWeaponId { get; set; }
    public UserWeapon? UserWeapon { get; set; }
}