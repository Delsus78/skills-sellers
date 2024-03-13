using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("seasons")]
public class Season
{
    public int Id { get; set; }
    public DateTime StartedDate { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime? EndedDate { get; set; }
    public int? WinnerId { get; set; }
    public string? RawJsonPlayerData { get; set; }
}