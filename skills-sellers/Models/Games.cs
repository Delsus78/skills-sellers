using System.Text.Json.Serialization;
using skills_sellers.Entities;

namespace skills_sellers.Models;

#region BASES CLASSES

[JsonDerivedType(typeof(GamesMachineResponse))]
[JsonDerivedType(typeof(GamesWordleResponse))]
[JsonDerivedType(typeof(GamesBossResponse))]
[JsonDerivedType(typeof(GamesBJResponse))]
public class GamesResponse
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Regles { get; set; }
}

public class GamesMachineResponse : GamesResponse
{
    public bool IsRepairing { get; set; }
    public int CreatiumPrice { get; set; }
    public int OrPrice { get; set; }
}

public class GamesBossResponse : GamesResponse
{
    public DateTime EndDate { get; set; }
    public DateTime StartDate { get; set; }
    public UserCardResponse BossCard { get; set; }
    public Dictionary<string,int> PlayersPower { get; set; } = new();
}

public class GamesBJResponse : GamesResponse
{
    public DateTime StartDate { get; set; }
    public BJGameResponse? Game { get; set; }
}

public class GamesWordleResponse : GamesResponse
{
    public int NbLetters { get; set; }
    public List<List<ReplaceTuple>> Words { get; set; }
    public bool IsWin { get; set; }
}

public class GamesPlayResponse
{
    public string Name { get; set; }
    public double Chances { get; set; }
    public double Results { get; set; }
    public bool? Win { get; set; }
    public List<List<ReplaceTuple>> Words { get; set; }
    public string Error { get; set; }
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
