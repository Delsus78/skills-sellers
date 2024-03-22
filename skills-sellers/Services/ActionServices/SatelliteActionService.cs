using System.Text;
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
    
    public SatelliteActionService(
        IUserBatimentsService userBatimentsService)
    {
        _userBatimentsService = userBatimentsService;
    }

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (user.Nourriture < 5 * userCards.Count)
            return (false, "Pas assez de nourriture");

        // Carte déjà en action
        if (userCards.Any(uc => uc.Action != null))
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "satellite", userCards.Count))
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
                User = user
            };

            await context.Actions.AddAsync(action);
            
            // consume resources
            user.Nourriture -= 5;

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
                { "nourriture", (5 * userCards.Count).ToString() }
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