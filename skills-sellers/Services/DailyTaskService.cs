using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services;


public interface IDailyTaskService
{
    Task ExecuteDailyTaskAsync();
    
    Task DailyResetCuisineAsync();
    Task DailyCheckAndDeleteNotifications();
}
public class DailyTaskService : IDailyTaskService
{
    private readonly DataContext _context;
    private readonly INotificationService _notificationService;
    
    public DailyTaskService(DataContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task ExecuteDailyTaskAsync()
    {
        var today = DateTime.Today;
        var logEntry = await _context.DailyTaskLog.SingleOrDefaultAsync(e => e.ExecutionDate == today);
        if (logEntry == null)
        {
            // Execute the task
            await DailyResetCuisineAsync();
            await DailyCheckAndDeleteNotifications();

            // Log the execution
            _context.DailyTaskLog.Add(new DailyTaskLog { ExecutionDate = today, IsExecuted = true });
            await _context.SaveChangesAsync();
        }
    }

    public async Task DailyResetCuisineAsync()
    {
        var usersBatimentsData = await _context.UserBatiments.ToListAsync();
        
        foreach (var userBatimentData in usersBatimentsData)
        {
            userBatimentData.NbCuisineUsedToday = 0;
        }
        
        // notify all users
        await _notificationService.SendNotificationToAll(new NotificationRequest("Cuisine", "Les cuisines ont été réinitialisées !"));

        await _context.SaveChangesAsync();
    }
    
    public async Task DailyCheckAndDeleteNotifications()
    {
        var notifications = await _context.Notifications.ToListAsync();
        foreach (var notification in notifications.Where(notification => notification.CreatedAt.AddDays(7) < DateTime.Now))
        {
            _context.Notifications.Remove(notification);
        }
        await _context.SaveChangesAsync();
    }
}