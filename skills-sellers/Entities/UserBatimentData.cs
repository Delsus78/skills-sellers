namespace skills_sellers.Entities;

public class UserBatimentData
{
    public int Id { get; set; }
    
    public int CuisineLevel { get; set; }
    public int NbCuisineUsedToday { get; set; }
    
    public int SalleSportLevel { get; set; }
    
    /// <summary>
    /// Le labo est pour l'instant non améliorable
    /// </summary>
    public int LaboLevel { get; set; }
    
    public int SpatioPortLevel { get; set; }
    
    public int NbBuyMarchandToday { get; set; }
    
    // One to one
    public User User { get; set; }
    
    public int UserId { get; set; }
}