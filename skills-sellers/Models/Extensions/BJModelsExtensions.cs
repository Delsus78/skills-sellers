using Microsoft.AspNetCore.SignalR;
using skills_sellers.Hubs;
using skills_sellers.Services.GameServices;

namespace skills_sellers.Models.Extensions;

public static class BJModelsExtensions
{
    public static int GetHandValue(this List<BJCard> hand)
    {
        var value = 0;
        var hasAs = false;
        foreach (var card in hand)
        {
            if (card.Value == 1)
                hasAs = true;
            value += card.Value;
        }

        if (hasAs && value + 10 <= 21)
            value += 10;

        return value;
    }
    
    public static BJCard DrawRandomCard(this List<BJCard> deck, bool visible)
    {
        var random = new Random();
        var index = random.Next(deck.Count);
        var card = deck[index];
        card.IsVisible = visible;
        deck.RemoveAt(index);
        
        return card;
    }
    
    public static void AddCards(this BlackJackEntity entity, IEnumerable<BJCard> cards)
    {
        var bjCards = cards.ToList();
        entity.Hand.AddRange(bjCards);
    }

    public static void AddPlayer(this List<BlackJackPlayer> players, int id, int mise,
        BlackJackGameContext gameContext)
    {
        players.Add(new BlackJackPlayer { Id = id, Mise = mise });
        gameContext.HubContext.Clients.All.SendAsync("PlayerAdded", gameContext.Game);
    }

    public static void DrawAction(this BlackJackPlayer player, BJGame game, 
        BlackJackGameContext gameContext)
    {
        var card = game.Deck.DrawRandomCard(true);
        player.AddCards(new[] { card });
        if (player.Hand.GetHandValue() > 21)
        {
            player.State = BlackJackState.Bust;
            gameContext.HubContext.Clients.All.SendAsync("PlayerLost", game.ToResponse());
        }
        else
        {
            gameContext.HubContext.Clients.All.SendAsync("PlayerDraw", game.ToResponse());
        }
    }
    
    public static void StandAction(this BlackJackPlayer player, 
        BlackJackGameContext gameContext)
    {
        player.State = BlackJackState.Stand;
        gameContext.HubContext.Clients.All.SendAsync("PlayerStand", gameContext.Game);
    }
    
    #region responses

    public static BJGameResponse ToResponse(this BJGame game)
        => new(game.Players.ToResponse(), 
            game.Hand.ToResponse(), 
            game.Status, 
            game.CurrentPlayerIdTurn, 
            game.Seed, 
            game.NextStepDate, 
            game.State?.ToString());
    
    public static List<BJCardResponse> ToResponse(this List<BJCard> cards) 
        => cards.Select(c => c.ToResponse()).ToList();
    
    public static BJCardResponse ToResponse(this BJCard c) 
        => new (c.IsVisible ? c.Name : null, 
            c.IsVisible ? c.Value : null, 
            c.IsVisible ? c.Color : null,
            c.IsVisible);
    
    public static BlackJackPlayerResponse ToResponse(this BlackJackPlayer player) 
        => new(player.Id, player.Mise, player.Hand.ToResponse(), player.State?.ToString());
    
    public static List<BlackJackPlayerResponse> ToResponse(this List<BlackJackPlayer> players) 
        => players.Select(p => p.ToResponse()).ToList();

    #endregion
}

