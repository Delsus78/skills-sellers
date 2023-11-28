using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Speciales;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Helpers.Bdd;

public class DataContext : DbContext
{
    #region DBSETS
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
    
    // Wordle game
    public DbSet<WordleGame> WordleGames { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    
    // Gift codes
    public DbSet<GiftCode> GiftCodes { get; set; }
    
    // Achievements
    public DbSet<Achievement> Achievements { get; set; }
    #endregion

    #region SPECIALS DBSETS

    // Christmas special
    public DbSet<Christmas> Christmas { get; set; }

    #endregion

    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        modelBuilder.Entity<Action>()
         .HasDiscriminator<string>("ActionType")
         .HasValue<ActionCuisiner>("Cuisine")
         .HasValue<ActionExplorer>("Explorer")
         .HasValue<ActionAmeliorer>("Ameliorer")
         .HasValue<ActionMuscler>("Muscler")
         .HasValue<ActionReparer>("Reparer")
         .HasValue<ActionSatellite>("Satellite");
        
        // adding user id to action
        modelBuilder.Entity<Action>()
            .HasOne(a => a.User);

        modelBuilder.Entity<UserCard>()
         .HasKey(uc => new { uc.UserId, uc.CardId });
        
        // link between user and stats
        modelBuilder.Entity<Stats>().HasOne(s => s.User)
            .WithOne(u => u.Stats)
            .HasForeignKey<Stats>(s => s.UserId);
        
        // link between user and achievements
        modelBuilder.Entity<Achievement>().HasOne(a => a.User)
            .WithOne(u => u.Achievement)
            .HasForeignKey<Achievement>(a => a.UserId);

        // link between user and wordle game
        modelBuilder.Entity<WordleGame>().HasOne(w => w.User)
            .WithOne(u => u.WordleGame)
            .HasForeignKey<WordleGame>(w => w.UserId);
        
        // christmas special
        modelBuilder.Entity<Christmas>().HasOne(c => c.User)
            .WithOne(u => u.Christmas)
            .HasForeignKey<Christmas>(c => c.UserId);
    }
}