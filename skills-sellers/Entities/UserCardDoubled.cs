namespace skills_sellers.Entities;

public class UserCardDoubled
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }

    public int CardId { get; set; }
    public Card Card { get; set; }
}