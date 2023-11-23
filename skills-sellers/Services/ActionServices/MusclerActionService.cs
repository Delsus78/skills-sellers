using System.Text;
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

    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // allow users to start multiple actions at the same time
        var actions = new List<Action>();
        
        // validate action
        foreach (var userCard in userCards)
        {
            // validation
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);
            
            if (!validation.valid)
                throw new AppException("Impossible de se muscler : " + validation.why, 400);

            // calculate action end time
            var endTime = CalculateActionEndTime(userCard.Competences.Force + 1);

            // random muscle
            var muscle = Randomizer.RandomMuscle();

            var action = new ActionMuscler
            {
                UserCards = new List<UserCard> { userCard },
                DueDate = endTime,
                User = user,
                Muscle = muscle
            };

            // actualise bdd
            await context.Actions.AddAsync(action);

            // consume resources
            user.Nourriture -= 1;

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
        var totalEndTime = new List<DateTime>();
        

        foreach (var userCard in userCards)
        {
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);
            if (!validation.valid)
            {
                errorMessages.AppendLine($"{userCard.Card.Name} : {validation.why}");
            }
            
            totalEndTime.Add(CalculateActionEndTime(userCard.Competences.Force + 1));
        }
            
            
        // allow users to start multiple actions at the same time
        return new ActionEstimationResponse
        {
            EndDates = totalEndTime,
            ActionName = "muscler",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up force", "100%" }
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", userCards.Count.ToString() }
            },
            Error = errorMessages.ToString()
        };
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        // only one card
        var userCard = action.UserCards.First();

        // up force competence
        if (userCard.Competences.Force < 10)
            userCard.Competences.Force += 1;
        else
        {
            // notify user
            _notificationService.SendNotificationToUser(userCard.User, new NotificationRequest
            (
                "Salle de sport",
                $"Votre carte {userCard.Card.Name} était déjà au max de force ! Votre nourriture a été remboursée."
            ), context);
            
            // refund resources
            userCard.User.Nourriture += 1;
        }

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