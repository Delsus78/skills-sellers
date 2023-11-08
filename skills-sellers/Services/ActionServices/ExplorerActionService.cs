using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class ExplorerActionService : IActionService
{
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IResourcesService _resourcesService;
    private readonly INotificationService _notificationService;
    private readonly IStatsService _statsService;
    private readonly IActionTaskService _actionTaskService;
    public ExplorerActionService(
        IUserBatimentsService userBatimentsService,
        IResourcesService resourcesService, 
        INotificationService notificationService, 
        IStatsService statsService,
        ActionTaskService actionTaskService)
    {
        _userBatimentsService = userBatimentsService;
        _resourcesService = resourcesService;
        _notificationService = notificationService;
        _statsService = statsService;
        _actionTaskService = actionTaskService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour explorer
        if (userCards.Count != 1)
            return (false, "Une carte seule est nécessaire pour explorer !");
        var userCard = userCards.First();

        // Carte déjà en action
        if (userCard.Action != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "spatioport"))
            return (false, "Batiment déjà plein");
        
        // Stats et ressources suffisantes ?
        if (user.Nourriture < 2)
            return (false, "Pas assez de nourriture");

        if (userCard.Competences.Exploration < 1)
            return (false, "Pas assez de compétences en exploration");

        return (true, "");
    }

    public async Task<Action> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
            throw new AppException("Impossible de partir en exploration : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime(userCards.First().Competences.Exploration);

        // Random planet
        var randomPlanet = Randomizer.RandomPlanet();
        
        var action = new ActionExplorer
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            IsReturningToHome = false,
            PlanetName = randomPlanet
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // stats
        _statsService.OnRocketLaunched(user.Id);
        
        // consume resources
        user.Nourriture -= 2;
        
        await context.SaveChangesAsync();

        // return response
        return action;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
        var userCard = userCards.First();
        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        
        // calculate action end time
        var endTime = CalculateActionEndTime(userCard.Competences.Exploration);
        var creatiumWinnableRange = _resourcesService
            .GetLimitsForForceStat(userCard.Competences.Force, "creatium");
        
            var orWinnableRange = _resourcesService
            .GetLimitsForForceStat(userCard.Competences.Force, "or");

            var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "explorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up exploration", "20%" },
                { "Creatium", $"{creatiumWinnableRange.min} - {creatiumWinnableRange.max}" },
                { "Or", $"{orWinnableRange.min} - {orWinnableRange.max}" },
                { "Chance de carte", userCard.Competences.Charisme * 10 + "%"}
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", "2" },
                { "Prend du temps à revenir", "15min"}
            },
            Error = !validation.valid ? "Impossible de partir en exploration : " + validation.why : null
        };
        
        // return response
        return action;
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionExplorer actionExplorer)
            throw new AppException("Action not found", 404);

        var user = action.User;
        
        var userCard = action.UserCards.First();

        if (actionExplorer.IsReturningToHome)
        {
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Explorer",
                $"Votre carte {userCard.Card.Name} est revenue de l'exploration !"
            ), context);

            // remove action if returning
            context.Actions.Remove(action);
        } 
        else
        {
            #region REWARDS
            
            // give resources and turn on the cardOpeningAvailable flag
            var creatiumWin = _resourcesService.GetRandomValueForForceStat(userCard.Competences.Force, "creatium");
            var orWin = _resourcesService.GetRandomValueForForceStat(userCard.Competences.Force, "or");
            user.Creatium += creatiumWin;
            user.Or += orWin;

            // stats
            _statsService.OnCreatiumMined(user.Id, creatiumWin);
            _statsService.OnOrMined(user.Id, orWin);
            
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Explorer",
                $"Votre carte {userCard.Card.Name} a gagné {creatiumWin} créatium et {orWin} or !"
            ), context);

            // chance to get a card based on charisme
            if (Randomizer.RandomPourcentageUp(userCard.Competences.Charisme * 10))
            {
                // notify user
                _notificationService.SendNotificationToUser(user, new NotificationRequest
                (
                    "Explorer",
                    $"Votre carte {userCard.Card.Name} a trouvé une nouvelle carte !"
                ), context);

                user.NbCardOpeningAvailable++;
            }
            else // stats
                _statsService.OnCardFailedCauseOfCharisme(user.Id);

            // // chance to up cuisine competence
            // - 20% de chance de up
            if (Randomizer.RandomPourcentageUp() && userCard.Competences.Exploration < 10)
            {
                userCard.Competences.Exploration += 1;
                // notify user
                _notificationService.SendNotificationToUser(user, new NotificationRequest
                (
                    "Compétence exploration",
                    $"Votre carte {userCard.Card.Name} a gagné 1 point de compétence en exploration !"
                ), context);
            }

            #endregion
            
            // update action to returning
            actionExplorer.IsReturningToHome = true;
            action.DueDate = CalculateActionEndTime(userCard.Competences.Exploration, true);
            
            // start timer for returning
            _ = _actionTaskService.StartNewTaskForAction(action);
        }

        return context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        // get user linked to action
        var user = action.User;

        if (action is not ActionExplorer actionExplorer)
            throw new AppException("Action not found", 404);
        
        if (actionExplorer.IsReturningToHome)
            throw new AppException("Vous ne pouvez pas annuler cette action !", 400);
        
        context.Actions.Remove(action);
        
        // refund resources
        user.Nourriture += 2;

        return context.SaveChangesAsync();
    }

    // Helpers
    private DateTime CalculateActionEndTime(int exploLevel, bool returning = false)
    {
        // l’exploration prendra 5h30 - le niveau x 30 minutes 
        return returning ? DateTime.Now.AddMinutes(15) : DateTime.Now.AddHours(5.5 - exploLevel * 0.5);
    }
}