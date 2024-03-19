namespace skills_sellers.Models;

public record NotificationResponse(int Id, string Title, string Message, DateTime CreatedAt, string Type, int? RelatedId);

public record NotificationRequest(string Title, string Message, string Type, int? RelatedId = null);