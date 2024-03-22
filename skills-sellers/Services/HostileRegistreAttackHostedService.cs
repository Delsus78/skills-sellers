using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Registres;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = System.Action;

namespace skills_sellers.Services;

public class HostileRegistreHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    
    public HostileRegistreHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Execute the task one time at startup
        ExecuteTask(null);
        
        var timeOfDay = DateTime.Now.TimeOfDay;
        var nextFullHour = TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));
        var delta = (nextFullHour - timeOfDay).TotalMilliseconds;
        _timer = new Timer(ExecuteTask, null, (int)delta, (int)TimeSpan.FromHours(2).TotalMilliseconds);
        return Task.CompletedTask;
    }
    
    private async void ExecuteTask(object? state)
    {
        Console.WriteLine("Starting Execution of Hostile Attacks tasks...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hostileRegistreAttackService = scope.ServiceProvider.GetRequiredService<IHostileRegistreAttackService>();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            await hostileRegistreAttackService.ExecuteHostileAttacksCheckAsync(context);
            Console.WriteLine("Hostile Attacks Check done.");
        } catch (Exception e)
        {
            Console.WriteLine($"Error while executing Hostile Attacks task : {e.Message}");
        }
        
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public interface IHostileRegistreAttackService
{
    Task ExecuteHostileAttacksCheckAsync(DataContext context);
}
public class HostileRegistreAttackService : IHostileRegistreAttackService
{
    private readonly INotificationService _notificationService;

    public HostileRegistreAttackService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ExecuteHostileAttacksCheckAsync(DataContext context)
    {
        // Check if the task has already been executed this hour
        var thisHour = DateTime.Now;
        var logEntry = await context.HostileRegistreAttacksLogs.SingleOrDefaultAsync(e => e.ExectuedAt.Date == thisHour.Date && e.ExectuedAt.Hour == thisHour.Hour);
        
        if (logEntry != null) return;
        
        Console.WriteLine($"[{thisHour}] Executing Hostile Attacks Check...");

        var users = context.Users
        .Include(u => u.UserCards)
            .ThenInclude(uc => uc.UserWeapon)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Action)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.UserWeapon)
            .ThenInclude(uw => uw.Weapon)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Competences)
        .Include(u => u.Registres)
        .Where(u => u.Registres.Any(r => r.Type == RegistreType.Hostile))
        .ToList();

        foreach (var user in users)
        {
            var hostileRegistres = user.Registres.OfType<RegistreHostile>().ToList();
            
            // nb of hostile registres = chance of attack (24/24 = 100%)
            var rdnNumber = Randomizer.RandomInt(0, 24);
            Console.WriteLine($"[HOSTILE TASK] Random number for {user.Pseudo} : {rdnNumber} out of {hostileRegistres.Count}/24");
            
            if (rdnNumber >= hostileRegistres.Count) continue;
            
            var allSatelliteFightingEntities = user.UserCards.Where(uc => uc.Action is ActionSatellite)
                .Select(c => new FightingEntity(c.Card.Name, c.ToResponse().Power, c.UserWeapon?.Affinity))
                .ToList();
                
            var allHostileFightingEntities = hostileRegistres.Select(
                    r => 
                        new FightingEntity(r.Name, 
                            r.CardPower + r.CardWeaponPower, // why not muscles
                            r.Affinity))
                .ToList();

            var results = WarHelpers.Battle(allSatelliteFightingEntities, allHostileFightingEntities);

            var msgNotif = !results.defenseWin ? 
                $"Vous avez perdu contre {allHostileFightingEntities.Count} registres hostiles. Les cartes suivantes ont perdu 1 point de compétence : \r\n"
                + WarHelpers.LoosingAnAttack(user, allHostileFightingEntities.Count) : "Vous avez gagné contre les registres hostiles.\r\n";
            msgNotif += results.fightReport;
                
            await _notificationService.SendNotificationToUser(user, new NotificationRequest(
                "Rapport attaque des planètes hostiles !", 
                msgNotif,
                ""), context);
        }
        
        // Log the execution
        context.HostileRegistreAttacksLogs.Add(new HostileRegistreAttacksLog { ExectuedAt = thisHour });
        
        await context.SaveChangesAsync();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="user"></param>
    /// <param name="nbOfHostileRegistres"></param>
    /// <returns>the message to print to the user</returns>
    
}