namespace skills_sellers.Entities;

public class DailyTaskLog
{
    public int Id { get; set; }
    public DateTime ExecutionDate { get; set; }
    public bool IsExecuted { get; set; }
}