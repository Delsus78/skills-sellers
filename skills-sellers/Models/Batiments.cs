namespace skills_sellers.Models;

public record UserBatimentResponse(
    int CuisineLevel,
    int NbCuisineUsedToday,
    int SalleSportLevel,
    int LaboLevel,
    int SpatioPortLevel);
    
public record UserBatimentRequest(
    int CuisineLevel,
    int NbCuisineUsedToday,
    int SalleSportLevel,
    int LaboLevel,
    int SpatioPortLevel);