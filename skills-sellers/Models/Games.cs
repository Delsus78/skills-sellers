using System.Text.Json.Serialization;

namespace skills_sellers.Models;

#region BASES CLASSES

[JsonDerivedType(typeof(GamesMachineResponse))]
[JsonDerivedType(typeof(GamesWordleResponse))]
public class GamesResponse
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Regles { get; set; }
}

public class GamesMachineResponse : GamesResponse
{
    public bool IsRepairing { get; set; }
}

public class GamesWordleResponse : GamesResponse
{
    public int NbLetters { get; set; }
    public List<List<ReplaceTuple>> Words { get; set; }
}

public class GamesPlayResponse
{
    public string Name { get; set; }
    public double Chances { get; set; }
    public double Results { get; set; }
    public bool? Win { get; set; }
    public List<List<ReplaceTuple>> Words { get; set; }
}

public class GamesRequest
{
    public string Name { get; set; }
    public int Bet { get; set; }
    public List<int> CardsIds { get; set; }
    public string? Word { get; set; }
}

public class ReplaceTuple
{
    public char Letter { get; set; }
    public int Status { get; set; }
    
    // implicit operator
    public static implicit operator ReplaceTuple((char c, int i) tuple)
        => new()
        {
            Letter = tuple.c,
            Status = tuple.i
        };
}

#endregion
