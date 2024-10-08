namespace skills_sellers.Entities.Registres;

public class RegistreHostile : Registre
{
    public int CardPower { get; set; }
    public int CardWeaponPower { get; set; }
    public WeaponAffinity? Affinity { get; set; }
    public override RegistreType Type => RegistreType.Hostile;
}