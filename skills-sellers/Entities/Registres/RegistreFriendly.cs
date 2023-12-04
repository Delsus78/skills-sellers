namespace skills_sellers.Entities.Registres;

public class RegistreFriendly : Registre
{
    public string ResourceOffer { get; set; }
    public string ResourceDemand { get; set; }
    public int ResourceOfferAmount { get; set; }
    public int ResourceDemandAmount { get; set; }
}