namespace skills_sellers.Entities;

public class FightReport
{
    public int Id { get; set; }
    public DateTime FightDate { get; set; }
    public List<string> Description { get; set; } = new();
}