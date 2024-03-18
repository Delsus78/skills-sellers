using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class NotificationsModelsExtensions
{
    public static NotificationResponse ToResponse(this Notification notification, string type = "", int? relatedId = null)
    {
        return new NotificationResponse(
            notification.Id, 
            notification.Title, 
            notification.Message, 
            notification.CreatedAt,
            type,
            relatedId);
    }
    
    public static Notification CreateNotification(this NotificationRequest model)
    {
        return new Notification
        {
            Title = model.Title,
            Message = model.Message,
            CreatedAt = DateTime.Now
        };
    }
}