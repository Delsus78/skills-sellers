namespace skills_sellers.Entities.Registres;

public class RegistreNeutral : Registre
{
    public bool? IsFavorite { get; set; } = false;
    public override RegistreType Type => RegistreType.Neutral;
}