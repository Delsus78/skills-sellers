using System.Diagnostics;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class WarActionService : IActionService
{
    private readonly INotificationService _notificationService;
    private readonly IWarService _warService;
    private readonly IActionTaskService _actionTaskService;

    public WarActionService(
        INotificationService notificationService, 
        IWarService warService, 
        IActionTaskService actionTaskService)
    {
        _notificationService = notificationService;
        _warService = warService;
        _actionTaskService = actionTaskService;
    }

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour partir en guerre
        if (userCards.Count < 1)
            return (false, "Une carte ou plus est nécessaire pour partir en guerre !");

        // une des carte est déjà en action
        if (userCards.Any(uc => uc.Action != null))
            return (false, "Une des cartes est déjà en action");

        // ressources insuffisantes
        if (user.Nourriture < userCards.Count * 4)
            return (false, "Ressources insuffisantes");
        
        // cards got weapons
        if (userCards.Any(uc => uc.UserWeaponId == null))
            return (false, "Une des cartes n'a pas d'arme");

        return (true, "");
    }
    
    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible d'envoyer en guerre : " + validation.why, 400);

        var action = new ActionGuerre
        {
            UserCards = userCards,
            DueDate = CalculateActionEndTime(WarStatus.EnAttente),
            User = user,
            WarId = model.WarId.Value
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // consume resources
        user.Nourriture -= userCards.Count(uc => uc.UserId == user.Id) * 4;
        
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
            EndDates = new List<DateTime> { CalculateActionEndTime() },
            ActionName = "Guerre",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>(),
            Couts = couts,
            Error = !validation.valid ? "Impossible d'envoyer en guerre : " + validation.why : null
        };

        // return response
        return action;
    }
    
    public async Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionGuerre actionGuerre)
            throw new AppException("Action not found", 404);
        
        // get war
        var war = context.Wars.Find(actionGuerre.WarId);
        if (war == null)
            throw new AppException("War not found", 404);

        if (war.Status == WarStatus.EnCours)
        {
            var notifMessage = $"La guerre {war.Id} est terminée !\r\n";
            
            // up force x2 if not 2 wars are already done this week
            if (context.Wars.Where(w => w.Status != WarStatus.Annulee && w.UserId == actionGuerre.User.Id)
                    .AsEnumerable()
                    .Count(w => w.CreatedAt.EstDansSemaineActuelle()) < 2)
            {
                var userCardsToUp = actionGuerre.UserCards
                    .Where(card => card.Competences.Force < 10)
                    .ToList();
                UpForceOfCards(userCardsToUp);
                
                notifMessage +=
                    $"Les cartes suivantes ont gagné 1 points de force : {string.Join(", ", userCardsToUp.Select(kvp => $"{kvp.Card.Name}"))}";
            }
            else
            {
                notifMessage += "Vous avez déjà effectué 2 guerres cette semaine, les cartes n'ont pas gagné de force.";
            }

            // remove action
            context.Actions.Remove(actionGuerre);
            await context.SaveChangesAsync();
            
            // notify user
            await _notificationService.SendNotificationToUser(actionGuerre.User, new NotificationRequest(
                    "Guerre terminée",
                    notifMessage,
                    "cards"),
                context);

            // change war status
            war.Status = WarStatus.Finie;
        }

        if (war.Status == WarStatus.EnAttente)
        {
            war.Status = WarStatus.EnCours;
            
            // start battle
            await _warService.StartBattle(war.Id, context);
            
            // reload action
            action.DueDate = CalculateActionEndTime(war.Status);
            _ = _actionTaskService.StartNewTaskForAction(action);
        }

        await context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionGuerre actionGuerre)
            throw new AppException("Action not found", 404);

        var war = context.Wars.Find(actionGuerre.WarId);
        if (war == null)
            throw new AppException("War not found", 404);
        
        if (war.Status != WarStatus.EnAttente)
            throw new AppException("Vous ne pouvez pas annuler cette action !", 400);
        
        var user = action.User;
        user.Nourriture += actionGuerre.UserCards.Count(uc => uc.UserId == user.Id) * 4;

        context.Actions.Remove(action);
        
        return context.SaveChangesAsync();
    }
    
    
    // Helpers
    
    private DateTime CalculateActionEndTime(WarStatus? type = null)
    {
        switch (type)
        {
            case null: // get whole time needed
                return DateTime.Now.AddHours(3);
            
            case WarStatus.EnAttente:
                return DateTime.Now.AddMinutes(30);

            default:
                return DateTime.Now.AddHours(2.5);
        }
    }
    
    private void UpForceOfCards(List<UserCard> userCardsToUp)
    {
        foreach (var card in userCardsToUp)
        {
            card.Competences.Force = card.Competences.Force + 1 > 10 ? 10 : +1;
        }
    }
}