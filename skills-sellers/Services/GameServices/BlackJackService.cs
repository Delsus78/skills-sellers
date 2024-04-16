using Microsoft.AspNetCore.SignalR;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Hubs;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services.GameServices;

public class BlackJackService : IGameService
{
    private static BJGame? ActualGame { get; set; }
    private static readonly object SyncLock = new ();
    private Timer? _timer;
    private readonly IHubContext<BlackJackHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notificationService;

    public BlackJackService(IHubContext<BlackJackHub> hubContext, 
        IServiceProvider serviceProvider, 
        INotificationService notificationService)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
    }

    public GamesResponse GetGameOfTheDay(int userId)
    {
        return new GamesBJResponse
        {
            Name = "BlackJack",
            Description = "Battez le croupier en obtenant le score le plus proche de 21 sans le dépasser !", 
            Regles = new Dictionary<string, string>
            {
                {"Le croupier tire des cartes jusqu'à atteindre un score de 17 ou plus.", "Si vous dépassez 21, vous perdez automatiquement."},
                {"Si vous obtenez un score de 21 avec vos deux premières cartes, vous avez un BlackJack.", "Chaque tête vaut 10 points, l'As vaut 1 ou 11 points selon votre choix, et les autres cartes valent leur valeur faciale."},
                {"Si vous avez un BlackJack, vous gagnez 2,5 fois votre mise.", "Si vous battez le croupier, vous gagnez 2 fois votre mise."},
                {"Si vous avez un score égal ou inférieur à celui du croupier, vous perdez votre mise.", "Si vous avez un score égal à celui du croupier, vous récupérez votre mise."}
            },
            Game = ActualGame?.ToResponse()
        };
    }

    public Task<GamesPlayResponse> PlayGameOfTheDay(User u, GamesRequest model)
    {
        lock (SyncLock)
        {
            using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
            var user = context.Users.Find(u.Id);
            if (user is null)
                throw new AppException("Utilisateur introuvable.", 404);

            ActualGame ??= CreateGame();

            var validation = CanJoinGame(user, model);
            if (!validation.valid)
                return Task.FromResult(new GamesPlayResponse
                {
                    Name = "BlackJack",
                    Error = validation.why
                });
            
            if (ActualGame.Players.All(p => p.Id != user.Id))
            {

                ActualGame.Players.AddPlayer(user.Id, model.Bet, new BlackJackGameContext(ActualGame.ToResponse(), _hubContext));
                user.Creatium -= model.Bet;
                user.Nourriture -= 1;
            }
            
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                "Vous avez misé *!"+ model.Bet +"!* sur une partie de BlackJack !", ""), context);
            
            // send game data
            _hubContext.Clients.All.SendAsync("Game", ActualGame.ToResponse());

            context.SaveChanges();
            
            return Task.FromResult(new GamesPlayResponse
            {
                Name = "BlackJack",
                Results = model.Bet * 2
            });
        }
    }


    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        var validation = CanJoinGame(user, model);
        
        if (!validation.valid)
            return new GamesPlayResponse
            {
                Name = "BlackJack",
                Error = validation.why
            };

        return new GamesPlayResponse
        {
            Name = "BlackJack",
            Results = model.Bet * 2
        };
    }

    public GamesPlayResponse TryPlay(User user, BlackJackAction action)
    {
        lock (SyncLock)
        {
            var player = ActualGame!.Players.FirstOrDefault(p => p.Id == user.Id);
            var validation = CanPlay(player);
            if (!validation.valid)
                return new GamesPlayResponse
                {
                    Name = "BlackJack",
                    Error = validation.why
                };

            switch (action)
            {
                case BlackJackAction.Draw:
                    player!.DrawAction(ActualGame, new BlackJackGameContext(ActualGame.ToResponse(), _hubContext));
                    break;
                case BlackJackAction.Stand:
                    player!.StandAction(new BlackJackGameContext(ActualGame.ToResponse(), _hubContext));
                    break;
            }

            if (player!.State is not null)
            {
                // cancel timer and start the next player
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                GameLoop(null);
            }
            
            
            return new GamesPlayResponse
            {
                Name = "BlackJack"
            };
        }
    }

    private (bool valid, string why) CanJoinGame(User user, GamesRequest model)
    {
        // check if the game is already started
        if (ActualGame is not null)
        {
            if (ActualGame is not { Status: GameStatus.EnAttente })
                return (false, "Une partie est déjà en cours.");
            
            // check if player is in game
            if (ActualGame.Players.Any(p => p.Id == user.Id))
                return (false, "Vous êtes déjà dans la partie.");
            
            // too much players
            if (ActualGame.Players.Count >= 5)
                return (false, "Trop de joueurs dans la partie. Attendez la suivante !");
        }
        
        // nouveau joueur, on ajoute le joueur en testant si il a assez de gold
        if (model.Bet <= 199)
            return (false, "Vous devez miser au moins 200 Creatium pour jouer.");
            
        if (user.Creatium < model.Bet)
            return (false, "Vous n'avez pas assez de creatium pour jouer.");
        
        if (user.Nourriture < 1)
            return (false, "Vous n'avez pas assez de nourriture pour jouer.");

        return (true, "");
    }
    
    private (bool valid, string why) CanPlay(BlackJackPlayer? player)
    {
        // get player in game
        if (player is null)
            return (false, "Vous n'êtes pas dans la partie.");
        
        // check if the game is already started
        if (ActualGame is not { Status: GameStatus.EnCours })
            return (false, "La partie n'est pas encore commencée.");

        // check if it's the player turn
        if (ActualGame.CurrentPlayerIdTurn != player.Id)
            return (false, "Ce n'est pas votre tour.");
        
        if (player.State is not null)
            return (false, "Vous avez déjà joué.");
        
        return (true, "");
    }
    
    private BJGame CreateGame()
    {
        var deck = GenerateDeck();
        var seed = new Random().Next();
        
        // start the game in 20 seconds
        _timer = new Timer(StartGame, null, (int) TimeSpan.FromSeconds(SecondsBeforeSteps.Start).TotalMilliseconds, Timeout.Infinite);
        
        return new BJGame
        {
            Deck = deck,
            Seed = seed,
            Random = new Random(seed),
            NextStepDate = DateTime.Now.AddSeconds(SecondsBeforeSteps.Start)
        };
    }
    
    private void StartGame(object? _)
    {
        // start the game
        ActualGame!.Status = GameStatus.EnCours;
        
        // draw cards for bank
        ActualGame.AddCards(new[]
        {
            ActualGame.Deck.DrawRandomCard(true),
            ActualGame.Deck.DrawRandomCard(false)
        });
        
        // draw cards for players
        foreach (var bjPlayer in ActualGame.Players)
        {
            bjPlayer.AddCards(new []
            {
                ActualGame.Deck.DrawRandomCard(true),
                ActualGame.Deck.DrawRandomCard(true)
            });
        }
        
        // check for blackjack
        foreach (var bjPlayer in ActualGame.Players.Where(bjPlayer => bjPlayer.Hand.GetHandValue() == 21))
        {
            bjPlayer.State = BlackJackState.BlackJack;
            _hubContext.Clients.All.SendAsync("BlackJack", ActualGame.ToResponse());
        }
        
        // start the game loop
        GameLoop(null);
    }

    private void GameLoop(object? state)
    {
        if (ActualGame is null)
            return;
        var actualPlayer = ActualGame.Players.Find(p => p.Id == ActualGame.CurrentPlayerIdTurn);
        if (actualPlayer is { State: null })
        {
            actualPlayer.State = BlackJackState.Stand;
        }
        
        var index = ActualGame.Players.FindIndex(p => p.Id == ActualGame.CurrentPlayerIdTurn);
        var nextIndex = index + 1;
        if (nextIndex >= ActualGame.Players.Count)
        {
            ActualGame.CurrentPlayerIdTurn = 0;
            BankPlay(0);
            return;
        }
        
        ActualGame.CurrentPlayerIdTurn = ActualGame.Players[nextIndex].Id;
        
        if (ActualGame.Players[nextIndex].State is not null)
        {
            GameLoop(null);
            return;
        }
        
        ActualGame.NextStepDate = DateTime.Now.AddSeconds(SecondsBeforeSteps.NextPlayerAuto);
        _hubContext.Clients.All.SendAsync("PlayerTurn", ActualGame.ToResponse());
        
        // start the timer for the next player
        _timer = new Timer(GameLoop, null, (int) TimeSpan.FromSeconds(SecondsBeforeSteps.NextPlayerAuto).TotalMilliseconds, Timeout.Infinite);
    }

    private void BankPlay(object? step)
    {
        var bankTurnEnded = false;
        switch (step)
        {
            case 0:
                
                foreach (var bjCard in ActualGame!.Hand)
                    bjCard.IsVisible = true;
                if (ActualGame.Hand.GetHandValue() == 21)
                    ActualGame.State = BlackJackState.BlackJack;
                
                step = 1;
                break;
            case 1:
                if (ActualGame!.Hand.GetHandValue() >= 17)
                    bankTurnEnded = true;
                else
                    ActualGame.AddCards(new[]
                    { ActualGame.Deck.DrawRandomCard(true) });
                break;
            
        }

        if (ActualGame.Hand.GetHandValue() > 21)
        {
            ActualGame.State = BlackJackState.Bust;
        }

        ActualGame.NextStepDate = DateTime.Now.AddSeconds(SecondsBeforeSteps.BankTurn);
        _hubContext.Clients.All.SendAsync("BankTurn", ActualGame!.ToResponse());
        
        if (!bankTurnEnded)
            _timer = new Timer(BankPlay, step, (int) TimeSpan.FromSeconds(SecondsBeforeSteps.BankTurn).TotalMilliseconds, Timeout.Infinite);
        else
            _timer = new Timer(EndGame, null, (int) TimeSpan.FromSeconds(SecondsBeforeSteps.BankTurn).TotalMilliseconds, Timeout.Infinite);
    }
    
    private async void EndGame(object? _)
    {
        var dataContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        var returnedWinners = new List<int>();

        foreach (var player in ActualGame!.Players)
        {
            var user = dataContext.Users.Find(player.Id);
            if (user is null)
                continue;
            
            switch (player.State, ActualGame.State)
            {
                case (BlackJackState.BlackJack, BlackJackState.BlackJack):
                    user.Creatium += player.Mise;
                    returnedWinners.Add(player.Id);
                    
                    // notify user
                    await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                        "Vous avez gagné *!"+ player.Mise +"!* creatium en effectuant un BlackJack en même temps que la banque !", ""), dataContext);
                    
                    break;
                case (BlackJackState.BlackJack, _):
                    user.Creatium += player.Mise * 5 / 2;
                    returnedWinners.Add(player.Id);
                    
                    // notify user
                    await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                        "Vous avez gagné *!"+ player.Mise * 5 / 2 +"!* creatium en effectuant un BlackJack !", ""), dataContext);
                    
                    break;
                case (BlackJackState.Stand, BlackJackState.Bust):
                    user.Creatium += player.Mise * 2;
                    returnedWinners.Add(player.Id);
                    
                    // notify user
                    await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                        "Vous avez gagné *!"+ player.Mise * 2 +"!* creatium en battant la banque !", ""), dataContext);
                    
                    break;
                case (BlackJackState.Stand, _):
                    if (player.Hand.GetHandValue() == ActualGame.Hand.GetHandValue())
                    {
                        user.Creatium += player.Mise;
                        returnedWinners.Add(player.Id);
                        
                        // notify user
                        await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                            "Vous avez récupéré votre mise de *!"+ player.Mise +"!* creatium !", ""), dataContext);
                        
                    } else if (player.Hand.GetHandValue() > ActualGame.Hand.GetHandValue())
                    {
                        user.Creatium += player.Mise * 2;
                        returnedWinners.Add(player.Id);
                        
                        // notify user
                        await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                            "Vous avez gagné *!"+ player.Mise * 2 +"!* creatium en battant la banque !", ""), dataContext);
                    }
                    else
                    {
                        // notify user
                        await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                            "Vous avez perdu votre mise de *!"+ player.Mise +"!* creatium en perdant contre la banque !", ""), dataContext);
                    }
                    break;
                
                case (BlackJackState.Bust, _):
                case (_,_):
                    // notify user
                    await _notificationService.SendNotificationToUser(user, new NotificationRequest("BLACKJACK",
                        "Vous avez perdu votre mise de *!"+ player.Mise +"!* creatium en dépassant 21 !", ""), dataContext);
                    break;
            }
        }
        
        
        await dataContext.SaveChangesAsync();
        
        lock (SyncLock)
        {
            ActualGame.NextStepDate = DateTime.Now.AddSeconds(SecondsBeforeSteps.EndGame);
            _hubContext.Clients.All.SendAsync("Winners", returnedWinners, ActualGame.ToResponse());
            _timer = new Timer(_ =>
            {
                _hubContext.Clients.All.SendAsync("EndGame");
            }, null, (int) TimeSpan.FromSeconds(SecondsBeforeSteps.EndGame).TotalMilliseconds, Timeout.Infinite);
        }
        
        ActualGame = null;
    }
    
    private List<BJCard> GenerateDeck()
    {
        var deck = new List<BJCard>();
        var colors = Enum.GetValues(typeof(CardColor));
        foreach (CardColor color in colors)
        {
            for (var i = 1; i <= 13; i++)
            {
                deck.Add(new BJCard
                {
                    Name = i switch
                    {
                        1 => "As",
                        11 => "Valet",
                        12 => "Dame",
                        13 => "Roi",
                        _ => i.ToString()
                    },
                    Value = i switch
                    {
                        >= 10 => 10,
                        _ => i
                    },
                    Color = color
                });
            }
        }
        return deck;
    }
}

public record BlackJackGameContext(BJGameResponse Game, IHubContext<BlackJackHub> HubContext);