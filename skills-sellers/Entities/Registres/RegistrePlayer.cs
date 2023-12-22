namespace skills_sellers.Entities.Registres;

public class RegistrePlayer : Registre
{
    public int RelatedPlayerId { get; set; }
    public User RelatedPlayer { get; set; }
    public override RegistreType Type => RegistreType.Player;
}