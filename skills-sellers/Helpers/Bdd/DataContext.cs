using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;

namespace skills_sellers.Helpers.Bdd;

public class DataContext : DbContext
{
    
    // cards data
    public DbSet<Card> Cards { get; set; }
    
    // user's data
    public DbSet<User> Users { get; set; }
    
    // user's cards
    public DbSet<UserCard> UserCards { get; set; }
    
    // hashed passwords with userIds
    public DbSet<AuthUser> AuthUsers { get; set; }
    
    // user's stats
    public DbSet<Stats> Stats { get; set; }
    
    // card's actions cuisiner
    public DbSet<ActionCuisiner> ActionsCuisiner { get; set; }
    
    // card's actions Ameliorer
    public DbSet<ActionAmeliorer> ActionsAmeliorer { get; set; }
    
    // card's actions Etudier
    public DbSet<ActionEtudier> ActionsEtudier { get; set; }
    
    // card's actions Explorer
    public DbSet<ActionExplorer> ActionsExplorer { get; set; }
    
    // card's actions Muscler
    public DbSet<ActionMuscler> ActionsMuscler { get; set; }
    

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