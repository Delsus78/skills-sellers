using System.Text;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class SatelliteActionService : IActionService
{
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IActionTaskService _actionTaskService;
    private readonly INotificationService _notificationService;
    
    public SatelliteActionService(
        IUserBatimentsService userBatimentsService, IActionTaskService actionTaskService, INotificationService notificationService)
    {
        _userBatimentsService = userBatimentsService;
        _actionTaskService = actionTaskService;
        _notificationService = notificationService;
    }

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        var foodPrice = userCards.Sum(u => u.Competences.GetPowerWithoutWeapon()) / 5;
        if (user.Nourriture < foodPrice)
            return (false, "Pas assez de nourriture");

        // Carte déjà en action
        if (userCards.Any(uc => uc.Action != null))
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "satellite", userCards.Count - 1))
            return (false, "Batiment déjà plein !");

        return (true, "");
    }

    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // allow users to start multiple actions at the same time
        var actions = new List<Action>();

        for (var index = 0; index < userCards.Count; index++)
        {
            var userCard = userCards[index];
            // validation
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);

            if (!validation.valid)
                throw new AppException("Impossible d'aller en orbite : " + validation.why, 400);

            // calculate action end time
            var endTime = CalculateActionEndTime().AddSeconds(-index);

            var action = new ActionSatellite
            {
                UserCards = new List<UserCard> { userCard },
                DueDate = endTime,
                User = user,
                IsAuto = false
            };

            await context.Actions.AddAsync(action);
            
            // consume resources
            user.Nourriture -= userCard.Competences.GetPowerWithoutWeapon() / 5;

            actions.Add(action);
        }

        await context.SaveChangesAsync();
        
        // return response
        return actions;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        var errorMessages = new StringBuilder();
        var endTime = CalculateActionEndTime();
        var foodPrice = userCards.Sum(u => u.Competences.GetPowerWithoutWeapon()) / 5;

        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
        {
            errorMessages.AppendLine(validation.why);
        }

        return new ActionEstimationResponse
        {
            EndDates = new List<DateTime> { endTime },
            ActionName = "satellite",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Protege votre planète des planètes hostiles", "" }
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", foodPrice.ToString() }
            },
            Error = errorMessages.ToString() // Retourne tous les messages d'erreur collectés
        };
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        // get user linked to action
        var user = action.User;
        context.Entry(user).Reference(u => u.UserBatimentData).Load();
        
        // actualise bdd and nb cuisine used today
        context.Actions.Remove(action);
        
        return context.SaveChangesAsync();
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionSatellite actionSatellite)
            throw new AppException("Action not found", 404);
        var user = action.User;
        
        context.Entry(action).Collection(a => a.UserCards)
            .Query()
            .Include(uc => uc.Card)
            .Load();
        
        if (actionSatellite.IsAuto)
        {
            if (user.Nourriture < 2 || user.Or < user.UserCards.Count * 2)
            {
                // notif user
                _notificationService.SendNotificationToUser(user, new NotificationRequest
                (
                    "Satellite",
                    $"Vous n'avez pas assez de ressources pour prolonger votre carte {action.UserCards.First().Card.Name} en orbite",
                    "onecard", action.UserCards.First().CardId
                ), context);
                
                // remove action
                context.Actions.Remove(actionSatellite);
                return context.SaveChangesAsync();
            }
            
            // consume resources
            user.Nourriture -= 2;
            user.Or -= user.UserCards.Count * 2;
            
            // start timer for returning
            actionSatellite.DueDate = CalculateActionEndTime();
            _ = _actionTaskService.StartNewTaskForAction(action);
            
            return context.SaveChangesAsync();
        }
        
        // remove action
        context.Actions.Remove(actionSatellite);
        return context.SaveChangesAsync();
    }

    // Helpers

    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddDays(1);
    }
}
