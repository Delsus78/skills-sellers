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
    Task SendNotificationToUser(User user, NotificationRequest notification);
    Task SendNotificationToAll(NotificationRequest notification);
    Task<IEnumerable<NotificationResponse>> GetNotifications(User user);
    Task DeleteNotification(User user, int notificationId);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly DataContext _context;
    
    public NotificationService(IHubContext<NotificationHub> hubContext, DataContext context)
    {
        _hubContext = hubContext;
        _context = context;
    }
    
    public async Task SendNotificationToUser(User user, NotificationRequest notification)
    {
        var notificationEntity = notification.CreateNotification();
        
        notificationEntity.User = user;
        
        // save notification in database
        var notifResulted = _context.Notifications.Add(notificationEntity).Entity;
        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("ReceiveNotification", notifResulted.ToResponse());
    }
    
    public async Task SendNotificationToAll(NotificationRequest notification)
    {
        var users = await _context.Users.ToListAsync();
        foreach (var user in users)
        {
            await SendNotificationToUser(user, notification);
        }
    }
    
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(User user)
    {
        var notifications = await _context.Notifications.Where(n => n.User.Id == user.Id).ToListAsync();
        return notifications.Select(n => n.ToResponse());
    }
    
    public async Task DeleteNotification(User user, int notificationId)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.User.Id == user.Id);
        if (notification == null)
            throw new AppException("Notification not found", 404);
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
    }
    
}