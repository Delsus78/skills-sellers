using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Registres;
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
    public DbSet<HostileRegistreAttacksLog> HostileRegistreAttacksLogs { get; set; }

    // Wordle game
    public DbSet<WordleGame> WordleGames { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    
    // Gift codes
    public DbSet<GiftCode> GiftCodes { get; set; }
    
    // Achievements
    public DbSet<Achievement> Achievements { get; set; }
    
    // Weapons
    public DbSet<Weapon> Weapons { get; set; }
    public DbSet<UserWeapon> UserWeapons { get; set; }
    
    // Cosmetics
    public DbSet<Cosmetic> Cosmetics { get; set; }
    public DbSet<UserCosmetic> UserCosmetics { get; set; }

    // Registres
    public DbSet<Registre> Registres { get; set; }
    public DbSet<UserRegistreInfo> UserRegistreInfos { get; set; }

    // Fights
    public DbSet<FightReport> FightReports { get; set; }
    public DbSet<War> Wars { get; set; }
    #endregion

    #region SPECIALS DBSETS

    // Christmas special
    public DbSet<Christmas> Christmas { get; set; }

    // Seasons
    public DbSet<Season> Seasons { get; set; }
    
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
         .HasValue<ActionSatellite>("Satellite")
         .HasValue<ActionGuerre>("Guerre")
         .HasValue<ActionBoss>("Boss");

        modelBuilder.Entity<Registre>()
            .HasDiscriminator<RegistreType>("Type")
            .HasValue<RegistrePlayer>(RegistreType.Player)
            .HasValue<RegistreHostile>(RegistreType.Hostile)
            .HasValue<RegistreNeutral>(RegistreType.Neutral)
            .HasValue<RegistreFriendly>(RegistreType.Friendly);
        
        // adding user id to action
        modelBuilder.Entity<Action>()
            .HasOne(a => a.User);
        
        modelBuilder.Entity<Registre>()
            .HasOne(r => r.User);

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