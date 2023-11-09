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
            await DailyResetUserRepairedMachine(context);

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
        await _notificationService.SendNotificationToAll(new NotificationRequest("Cuisine", "Les cuisines ont été réinitialisées !"), context);
        await _notificationService.SendNotificationToAll(new NotificationRequest("Marchand BonnBouff", "Le marchand a été réinitialisé !"), context);

        await context.SaveChangesAsync();
    }
    
    private async Task DailyResetUserRepairedMachine(DataContext context)
    {
        var usersWithRepairedMachine = await context.Users.Where(u => u.StatRepairedObjectMachine != -1).ToListAsync();

        var count = 0;
        foreach (var user in usersWithRepairedMachine)
        {
            user.StatRepairedObjectMachine = -1;
            count++;
        }
        Console.WriteLine($"DailyTask : {count} users StatRepairedObjectMachine reset");
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
}