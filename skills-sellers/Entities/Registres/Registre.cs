namespace skills_sellers.Entities.Registres;

public abstract class Registre
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public virtual RegistreType Type { get; protected set; }
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