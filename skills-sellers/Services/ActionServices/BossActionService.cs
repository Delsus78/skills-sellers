using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using skills_sellers.Services.GameServices;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class BossActionService : IActionService
{
    
    private readonly INotificationService _notificationService;
    private readonly BossService _bossService;

    public BossActionService(INotificationService notificationService, BossService bossService)
    {
        _notificationService = notificationService;
        _bossService = bossService;
    }

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour partir en guerre
        if (userCards.Count < 1)
            return (false, "Une carte ou plus est nécessaire pour partir en guerre !");

        // une des carte est déjà en action
        if (userCards.Any(uc => uc.Action != null && uc.Action is not ActionBoss))
            return (false, "Une des cartes est déjà en action");

        return (true, "");
    }
    
    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var replacementUser = context.Users.First();
        
        if (model.DueDate == null)
            throw new AppException("DueDate are required", 500);
        
        var action = new ActionBoss
        {
            UserCards = new List<UserCard>(),
            DueDate = model.DueDate.Value,
            User = replacementUser
        };
        
        await context.Actions.AddAsync(action);
        await context.SaveChangesAsync();

        // return response
        return new List<Action> { action };
    }
    
    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        
        // couts
        var couts = new Dictionary<string, string>
        {
            {"nourritures", -userCards.Count * 4 + ""}
        };
        
        var action = new ActionEstimationResponse
        {
            EndDates = new List<DateTime> { DateTime.Today.AddHours(20) },
            ActionName = "Boss",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>(),
            Couts = couts,
            Error = !validation.valid ? "Impossible d'aller combattre : " + validation.why : null
        };

        // return response
        return action;
    }
    
    public async Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionBoss actionBoss)
            throw new AppException("Action is not a boss action", 500);
        
        // get all user who have cards in the action
        var users = actionBoss.UserCards.Select(uc => uc.User).Distinct().ToList();
        var cards = actionBoss.UserCards;
        
        // get all power by playerNames
        
        var powerByPlayer = cards
            .GroupBy(uc => uc.User.Pseudo)
            .ToDictionary(g => g.Key, g => g.Sum(uc => uc.ToResponse().Power));
        
        // get today game
        var game = _bossService.GetGameOfTheDay(0) as GamesBossResponse;
        
        // is boss dead
        var totalPower = powerByPlayer.Values.Sum();
        string notifyMessage;
        
        if (totalPower >= game.BossCard.Power)
        {
            notifyMessage = "Le boss a été vaincu !\r\n";
        }
        else
        {
            notifyMessage = "Le boss a écrasé les joueurs !\r\nMalheureusement, il a survécu...\r\n";
        }
        
        // remove action
        context.UserCards
            .Where(uc => action != null && uc.Action.Id == action.Id).ToList()
            .ForEach(uc => uc.Action = null);
        context.Actions.Remove(action);
        
        foreach (var user in users)
        {
            var notifyMessageUser = notifyMessage;
            if (totalPower >= game.BossCard.Power)
            {
                var userPower = powerByPlayer[user.Pseudo];
                var multiplicator = userPower / 3;

                var stringReward = WarHelpers.GetRandomWarLoot(user, multiplicator) + "\r\n";
                stringReward += WarHelpers.GetRandomWarLoot(user, multiplicator) + "\r\n";
                notifyMessageUser += $"Vous avez gagné : \r\n{stringReward}\r\n+200 SCORE";
                user.Score += 200;
            }
            else
            {
                notifyMessageUser += "+100 SCORE\r\n";
                user.Score += 100;
            }
            
            // notify user
            await _notificationService.SendNotificationToUser(user, new NotificationRequest(
                "BOSS - " + game.BossCard.Name,
                notifyMessageUser
                ,"cards"), context);
        }
        
        await context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        throw new AppException("This action can't be deleted", 500);
    }
}