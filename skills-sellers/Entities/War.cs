using System.ComponentModel.DataAnnotations.Schema;

namespace skills_sellers.Entities;

[Table("wars")]
public class War
{
    public int Id { get; set; }
    public int RegistreTargetId { get; set; }
    public int? UserTargetId { get; set; }
    public List<int> UserAllyIds { get; set; }
    public WarStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}

public enum WarStatus
{
    EnAttente,
    EnCours,
    Annulee,
    Finie
}