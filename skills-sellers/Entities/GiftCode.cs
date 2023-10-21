namespace skills_sellers.Entities;

public class GiftCode
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public int NbCards { get; set; }
    public int NbCreatium { get; set; }
    public int NbOr { get; set; }
    
    public bool Used { get; set; }
}