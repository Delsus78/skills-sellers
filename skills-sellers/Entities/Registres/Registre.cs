namespace skills_sellers.Entities.Registres;

public abstract class Registre
{
    public static int MaxRegistreHostile = 24;
    public static int MaxRegistreFriendly = 24;
    
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public RegistreType Type { get; set; }
    public DateTime EncounterDate { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}

public enum RegistreType
{
    Player,
    Hostile,
    Neutral,
    Friendly
}