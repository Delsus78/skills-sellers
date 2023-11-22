namespace skills_sellers.Entities;

public abstract class Action
{
    public int Id { get; set; }
    
    // Due Date
    public DateTime DueDate { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
    
    // One to Many
    public List<UserCard> UserCards { get; set; } = new();
    public int UserId { get; set; }
    public User User { get; set; }

    public override string ToString()
    {
        return $"Action {Id} - DDate : {DueDate} - [{UserId}]: \n {UserCards.Aggregate("", (current, userCard) => current + $"Card {userCard.CardId} - {userCard.Card.Name} \n")}";
    }
}