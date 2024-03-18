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
    Task SendWarNotificationToUser(User user, NotificationRequest notification, DataContext context);
    Task SendNotificationToAll(NotificationRequest notification, DataContext context);
    Task<IEnumerable<NotificationResponse>> GetNotifications(User user);
    Task DeleteNotifications(User user, List<int> notificationIds);
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
        
        // log
        Console.Out.WriteLine($"[NOTIFICATION] {user.Id}: {notificationEntity.Title} | {notificationEntity.Message} | {notificationEntity.CreatedAt}");
        
        await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("ReceiveNotification", notifResulted.ToResponse(notification.Type, notification.RelatedId));
    }
    
    public async Task SendWarNotificationToUser(User user, NotificationRequest notification, DataContext context)
    {
        var notificationEntity = notification.CreateNotification();
        
        notificationEntity.User = user;
        
        // save notification in database
        var notifResulted = context.Notifications.Add(notificationEntity).Entity;
        
        // log
        Console.Out.WriteLine($"[WAR NOTIFICATION] {user.Id}: {notificationEntity.Title} | {notificationEntity.Message} | {notificationEntity.CreatedAt}");
        
        await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("WarNotification", notifResulted.ToResponse(notification.Type, notification.RelatedId));
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
    
    public async Task DeleteNotifications(User user, List<int> notificationIds)
    {
        await using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var notifications = await context.Notifications.Where(n => notificationIds.Contains(n.Id)).ToListAsync();
        if (notifications.Count == 0)
            throw new AppException("No Notification was found for this user", 404);
        
        context.Notifications.RemoveRange(notifications);
        await context.SaveChangesAsync();
    }
    
}