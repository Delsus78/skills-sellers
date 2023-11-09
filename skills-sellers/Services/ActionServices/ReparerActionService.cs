using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class ReparerActionService : IActionService
{
    private readonly INotificationService _notificationService;

    public ReparerActionService(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (user.StatRepairedObjectMachine != -1)
            throw new AppException("Vous avez déjà réparé la machine !", 400);
        
        if (userCards.Count < 1)
            throw new AppException("Vous devez jouer une carte minimum !", 400);
        
        // cards already in action ?
        if (userCards.Any(c => c.Action != null))
            throw new AppException("Une de vos cartes est déjà en action !", 400);
        
        return (true, "");
    }

    public async Task<Action> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible ! " + validation.why, 400);
        
        // set cards in action
        var action = new ActionReparer
        {
            UserCards = userCards,
            DueDate = CalculateActionEndTime(),
            RepairChances = model.RepairChances,
            User = user
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        await context.SaveChangesAsync();

        // return response
        return action;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible ! " + validation.why, 400);
        
        // calculate action end time and resources
        var endTime = CalculateActionEndTime();
        
        return new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "ameliorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Machine accessible", "" }
            },
            Couts = new Dictionary<string, string>
            {
            },
            Error = !validation.valid ? "Impossible de réparer la machine : " + validation.why : null
        };
    }

    public async Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        if (action is not ActionReparer actionReparer)
            throw new AppException("Action is not a reparer action", 400);
        
        // set repaired depending on the chances
        var random = new Random();
        var chances = random.Next(0, 100);
        if (chances < actionReparer.RepairChances)
        {
            user.StatRepairedObjectMachine = 0;
            await context.SaveChangesAsync();
            
            // notify user
            await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Réparation terminée", 
                    $"La réparation de la machine est terminée ! Vous pouvez maintenant l'utiliser !"), 
                context);
        }
        else await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Réparation échouée",
                    $"La réparation de la machine a échouée ! Vous pouvez retenter votre chance !"),
                context);
        
        // remove action
        context.Actions.Remove(action);
        
        await context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        context.Actions.Remove(action);
        
        // refund
        user.StatRepairedObjectMachine = -1;

        return context.SaveChangesAsync();
    }

    // Helpers
    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddHours(1);
    }
}