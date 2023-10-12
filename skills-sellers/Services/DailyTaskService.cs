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
    private DataContext _context;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;
    
    public DailyTaskService(DataContext context, INotificationService notificationService, IServiceProvider serviceProvider)
    {
        _context = context;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteDailyTaskAsync()
    {
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
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
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var usersBatimentsData = await _context.UserBatiments.ToListAsync();
        
        foreach (var userBatimentData in usersBatimentsData)
        {
            userBatimentData.NbCuisineUsedToday = 0;
        }
        
        // notify all users
        await _notificationService.SendNotificationToAll(new NotificationRequest("Cuisine", "Les cuisines ont été réinitialisées !"), _context);

        await _context.SaveChangesAsync();
    }
    
    public async Task DailyCheckAndDeleteNotifications()
    {
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var notifications = await _context.Notifications.ToListAsync();
        foreach (var notification in notifications.Where(notification => notification.CreatedAt.AddDays(7) < DateTime.Now))
        {
            _context.Notifications.Remove(notification);
        }
        await _context.SaveChangesAsync();
    }
}