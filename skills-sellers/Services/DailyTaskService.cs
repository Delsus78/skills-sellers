using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services;


public interface IDailyTaskService
{
    Task ExecuteDailyTaskAsync(DataContext context);
}
public class DailyTaskService : IDailyTaskService
{
    private readonly INotificationService _notificationService;
    
    public DailyTaskService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ExecuteDailyTaskAsync(DataContext context)
    {
        var today = DateTime.Today;
        var logEntry = await context.DailyTaskLog.SingleOrDefaultAsync(e => e.ExecutionDate == today.Date);
        if (logEntry == null)
        {
            Console.WriteLine("Daily task needed");
            
            // Execute the task
            await DailyResetBatimentDataAsync(context);
            await DailyCheckAndDeleteNotifications(context);
            await DailyCheckAndDeleteFightReports(context);

            // Log the execution
            context.DailyTaskLog.Add(new DailyTaskLog { ExecutionDate = today.Date, IsExecuted = true });
            await context.SaveChangesAsync();
        }
    }

    private async Task DailyResetBatimentDataAsync(DataContext context)
    {
        var usersBatimentsData = await context.UserBatiments.ToListAsync();

        var count = 0;
        foreach (var userBatimentData in usersBatimentsData)
        {
            userBatimentData.NbCuisineUsedToday = 0;
            userBatimentData.NbBuyMarchandToday = 0;
            count++;
        }
        
        Console.WriteLine($"DailyTask : {count} users batiments data reset");
        
        // notify all users
        await _notificationService.SendNotificationToAll(new NotificationRequest("Daily reset", "Les cuisines ont été réinitialisées !\r\nLe marchand a été réinitialisé !"), context);

        await context.SaveChangesAsync();
    }

    private async Task DailyCheckAndDeleteNotifications(DataContext context)
    {
        var notifications = await context.Notifications
            .Where(notification => notification.CreatedAt.AddDays(7).Date <= DateTime.Now.Date && !notification.Title.Contains("DM") && !notification.Title.Contains("SPECIAL"))
            .ToListAsync();
        var count = 0;
        foreach (var notification in notifications)
        {
            context.Notifications.Remove(notification);
            count++;
        }
        Console.Out.WriteLine($"DailyTask : Deleted {count} notifications");
        await context.SaveChangesAsync();
    }
    
    private async Task DailyCheckAndDeleteFightReports(DataContext context)
    {
        var fightReports = await context.FightReports
            .Where(fightReport => fightReport.FightDate.AddDays(3).Date <= DateTime.Now.Date)
            .ToListAsync();
        var count = 0;
        foreach (var fightReport in fightReports)
        {
            context.FightReports.Remove(fightReport);
            count++;
        }
        Console.Out.WriteLine($"DailyTask : Deleted {count} fight reports");
        await context.SaveChangesAsync();
    }
}