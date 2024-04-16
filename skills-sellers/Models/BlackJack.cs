namespace skills_sellers.Models;

public abstract class BlackJackEntity
{
    public int Id { get; set; }
    public BlackJackType Type { get; set; }
    public List<BJCard> Hand { get; set; } = new();
    public BlackJackState? State { get; set; }
}

public enum BlackJackType
{
    Game,
    Player
}

public class BJGame : BlackJackEntity
{
    public List<BlackJackPlayer> Players { get; set; } = new();
    public List<BJCard> Deck { get; set; } = new();
    public GameStatus Status { get; set; } = GameStatus.EnAttente;
    public int CurrentPlayerIdTurn { get; set; }
    public int Seed { get; set; }
    public Random Random { get; init; }
    public DateTime? NextStepDate { get; set; }
}

public static class SecondsBeforeSteps
{
    public const int Start = 20;
    public const int TrueStart = 5;
    public const int NextPlayerAuto = 120;
    public const int BankTurn = 5;
    public const int EndGame = 5;
}

public class BlackJackPlayer : BlackJackEntity
{
    public int Mise { get; set; }
}

public record BlackJackPlayerResponse(int Id, int Mise, List<BJCardResponse> Hand, String? State);

public record BJGameResponse(List<BlackJackPlayerResponse> Players,
    List<BJCardResponse> BankHand, GameStatus Status, int CurrentPlayerTurn, int Seed, DateTime? NextStepDate, String? State);

public class BJCard
{
    public string Name { get; set; }
    public int Value { get; set; }
    public CardColor Color { get; set; }
    public bool IsVisible { get; set; } = true;
}

public record BJCardResponse(string? Name, int? Value, CardColor? Color, bool IsVisible);

public enum CardColor
{
    Coeur,
    Carreau,
    Pique,
    Trèfle
}

public enum GameStatus
{
    EnAttente,
    EnCours
}

public enum BlackJackState
{
    BlackJack,
    Bust,
    Stand
}

public enum BlackJackAction
{
    Draw,
    Stand
}