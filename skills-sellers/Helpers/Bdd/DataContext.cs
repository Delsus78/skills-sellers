using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;

namespace skills_sellers.Helpers.Bdd;

public class DataContext : DbContext
{
    public DbSet<Card> Cards { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserCard> UserCards { get; set; }
    
    // hashed passwords with userIds
    public DbSet<AuthUser> AuthUsers { get; set; }
    public DbSet<Stats> Stats { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCard>()
            .HasKey(uc => new { uc.UserId, uc.CardId });
        
            // link between user and stats
        modelBuilder.Entity<Stats>().HasOne(s => s.User)
            .WithOne(u => u.Stats)
            .HasForeignKey<Stats>(s => s.UserId);
    }
}