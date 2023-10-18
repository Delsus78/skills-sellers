namespace skills_sellers.Models;

#region BASES CLASSES

public class GamesResponse
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Regles { get; set; }
}

public class GamesPlayResponse
{
    public string Name { get; set; }
    public double Chances { get; set; }
    public double Results { get; set; }
    public bool? Win { get; set; }
}

public class GamesRequest
{
    public string Name { get; set; }
    public int Bet { get; set; }
    public List<int> CardsIds { get; set; }
}

#endregion
