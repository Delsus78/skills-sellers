using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Helpers.Bdd;

public class DataContext : DbContext
{
    
    // cards data
    public DbSet<Card> Cards { get; set; }
    
    // user's data
    public DbSet<User> Users { get; set; }
    
    // user's cards
    public DbSet<UserCard> UserCards { get; set; }
    public DbSet<UserCardDoubled> UserCardDoubleds { get; set; }

    // hashed passwords with userIds
    public DbSet<AuthUser> AuthUsers { get; set; }
    
    // user's stats
    public DbSet<Stats> Stats { get; set; }
    
    // Actions base
    public DbSet<Action> Actions { get; set; }

    // batiments
    public DbSet<UserBatimentData> UserBatiments { get; set; }
    
    // Daily task log
    public DbSet<DailyTaskLog> DailyTaskLog { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        modelBuilder.Entity<Action>()
         .HasDiscriminator<string>("ActionType")
         .HasValue<ActionCuisiner>("Cuisine")
         .HasValue<ActionExplorer>("Explorer")
         .HasValue<ActionAmeliorer>("Ameliorer")
         .HasValue<ActionMuscler>("Muscler");
        
        // adding user id to action
        modelBuilder.Entity<Action>()
            .HasOne(a => a.User);

        modelBuilder.Entity<UserCard>()
         .HasKey(uc => new { uc.UserId, uc.CardId });

        // link between user and stats
        modelBuilder.Entity<Stats>().HasOne(s => s.User)
            .WithOne(u => u.Stats)
            .HasForeignKey<Stats>(s => s.UserId);
    }
}