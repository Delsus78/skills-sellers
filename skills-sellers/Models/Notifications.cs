namespace skills_sellers.Models;

public record NotificationResponse(int Id, string Title, string Message, DateTime CreatedAt);

public record NotificationRequest(string Title, string Message);