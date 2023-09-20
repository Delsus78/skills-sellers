using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;

namespace skills_sellers.Helpers.Bdd;

public class DataContext : DbContext
{
    public DbSet<Card> Cards { get; set; }
    public DbSet<User> Users { get; set; }
    
    // hashed passwords with userIds
    public DbSet<AuthUser> AuthUsers { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
}