namespace skills_sellers.Entities.Actions;

public class ActionAmeliorer : Action
{
    public string? BatimentToUpgrade { get; set; }
    
    public int? WeaponToUpgradeId { get; set; }
}