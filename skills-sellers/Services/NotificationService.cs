using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Hubs;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
namespace skills_sellers.Services;

public interface INotificationService
{
    Task SendNotificationToUser(User user, NotificationRequest notification, DataContext context);
    Task SendNotificationToAll(NotificationRequest notification, DataContext context);
    Task<IEnumerable<NotificationResponse>> GetNotifications(User user);
    Task DeleteNotification(User user, int notificationId);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private IServiceProvider _serviceProvider;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        IServiceProvider serviceProvider)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
    }
    
    public async Task SendNotificationToUser(User user, NotificationRequest notification, DataContext context)
    {
        var notificationEntity = notification.CreateNotification();
        
        notificationEntity.User = user;
        
        // save notification in database
        var notifResulted = context.Notifications.Add(notificationEntity).Entity;
        
        await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("ReceiveNotification", notifResulted.ToResponse());
    }
    
    public async Task SendNotificationToAll(NotificationRequest notification, DataContext context)
    {
        var users = await context.Users.ToListAsync();
        foreach (var user in users)
        {
            await SendNotificationToUser(user, notification, context);
        }
    }
    
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(User user)
    {
        await using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var notifications = await context.Notifications.Where(n => n.User.Id == user.Id).ToListAsync();
        return notifications.Select(n => n.ToResponse());
    }
    
    public async Task DeleteNotification(User user, int notificationId)
    {
        await using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.User.Id == user.Id);
        if (notification == null)
            throw new AppException("Notification not found", 404);
        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();
    }
    
}