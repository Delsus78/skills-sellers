namespace skills_sellers.Models;

public record UserBatimentResponse(
    int CuisineLevel,
    int NbCuisineUsedToday,
    int SalleSportLevel,
    int ActualSalleSportUsed,
    int LaboLevel,
    int ActualLaboUsed,
    int SpatioPortLevel,
    int ActualSpatioPortUsed,
    int NbBuyMarchandToday,
    int SatelliteLevel,
    int ActualSatelliteUsed,
    int NbBuyMarchandMaxPerDay);
    
public record UserBatimentRequest(
    int CuisineLevel,
    int NbCuisineUsedToday,
    int SalleSportLevel,
    int LaboLevel,
    int SpatioPortLevel,
    int SatelliteLevel,
    int NbBuyMarchandToday);