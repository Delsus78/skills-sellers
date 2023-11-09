using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class MusclerActionService : IActionService
{
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly INotificationService _notificationService;
    
    public MusclerActionService(
        IUserBatimentsService userBatimentsService,
        INotificationService notificationService)
    {
        _userBatimentsService = userBatimentsService;
        _notificationService = notificationService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour se muscler
        if (userCards.Count != 1)
            return (false, "Une seule carte par séance de muscu");
        var userCard = userCards.First();
        
        // Carte déjà en action
        if (userCard.Action != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "salledesport"))
            return (false, "Bâtiment déjà plein");
        
        // Stats et ressources suffisantes ?
        if (user.Nourriture < 1)
            return (false, "Pas assez de nourriture");
        
        if (userCard.Competences.Force >= 10)
            return (false, "Force max atteinte");
        
        return (true, "");
    }

    public async Task<Action> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
            throw new AppException("Impossible de se muscler : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime(userCards.First().Competences.Force  + 1);
        
        // random muscle
        var muscle = Randomizer.RandomMuscle();
        
        var action = new ActionMuscler
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            Muscle = muscle
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // consume resources
        user.Nourriture -= 1;
        
        await context.SaveChangesAsync();

        // return response
        return action;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);

        // calculate action end time
        var endTime = CalculateActionEndTime(userCards.First().Competences.Force  + 1);

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "muscler",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up force", "100%" }
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", "1" }
            },
            Error = !validation.valid ? "Impossible d'aller pousser à la salle : " + validation.why : null
        };
        
        // return response
        return action;
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        // only one card
        var userCard = action.UserCards.First();

        // up force competence
        
        userCard.Competences.Force += 1;

        context.UserCards.Update(userCard);

        // remove action
        context.Actions.Remove(action);

        // notify user
        _notificationService.SendNotificationToUser(userCard.User, new NotificationRequest
        (
            "Salle de sport",
            $"Votre carte {userCard.Card.Name} a gagné 1 point de force !"
        ), context);

        return context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        context.Actions.Remove(action);
        
        // refund resources
        user.Nourriture += 1;
        
        return context.SaveChangesAsync();
    }

    // Helpers
    private DateTime CalculateActionEndTime(int forceLevelToUp)
    {
        return DateTime.Now.AddHours(forceLevelToUp);
    }
}