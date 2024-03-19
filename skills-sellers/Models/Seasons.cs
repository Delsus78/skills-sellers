namespace skills_sellers.Models;

public record SeasonResponse(
    int Id,
    DateTime StartedDate, 
    DateTime ScheduledEndDate, 
    DateTime? EndedDate, 
    string? Winner, 
    int? WinnerId, 
    string? Informations);
    
public record SeasonRequest(TimeSpan Duration);