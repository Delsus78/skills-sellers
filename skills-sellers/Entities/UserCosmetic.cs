namespace skills_sellers.Entities;

public class UserCosmetic
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CosmeticId { get; set; }
    public Cosmetic Cosmetic { get; set; }
    public int CoordinateX { get; set; }
    public int CoordinateY { get; set; }
    public int Size { get; set; }
}