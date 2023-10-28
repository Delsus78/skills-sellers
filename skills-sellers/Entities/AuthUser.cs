using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace skills_sellers.Entities;

[Table("auth_users")]
[PrimaryKey("UserId")]
public class AuthUser
{
    public int UserId { get; set; }
    public string Hash { get; set; }
    public string Role { get; init; }
}