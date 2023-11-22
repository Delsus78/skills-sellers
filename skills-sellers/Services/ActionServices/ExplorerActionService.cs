using System.Text;
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
        IActionTaskService actionTaskService)
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

    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // allow users to start multiple actions at the same time
        var actions = new List<Action>();

        foreach (var userCard in userCards)
        {
            // validate action
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);
            
            if (!validation.valid)
                throw new AppException("Impossible de partir en exploration : " + validation.why, 400);

            // calculate action end time
            var endTime = CalculateActionEndTime(userCard.Competences.Exploration);

            // Random planet
            var randomPlanet = Randomizer.RandomPlanet();

            var action = new ActionExplorer
            {
                UserCards = new List<UserCard> { userCard },
                DueDate = endTime,
                UserId = user.Id,
                IsReturningToHome = false,
                PlanetName = randomPlanet
            };

            // actualise bdd
            await context.Actions.AddAsync(action);

            // consume resources
            user.Nourriture -= 2;

            actions.Add(action);
        }
        
        await context.SaveChangesAsync();

        // return response
        return actions;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
        var totalCreatiumMin = 0;
        var totalCreatiumMax = 0;
        var totalOrMin = 0;
        var totalOrMax = 0;
        var totalCharisme = 0;
        var totalEndTime = new List<DateTime>();
        var errorMessages = new StringBuilder();

        foreach (var userCard in userCards)
        {
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);
            if (!validation.valid)
            {
                errorMessages.AppendLine($"{userCard.Card.Name} : {validation.why}");
            }

            var creatiumRange = _resourcesService.GetLimitsForForceStat(userCard.Competences.Force, "creatium");
            var orRange = _resourcesService.GetLimitsForForceStat(userCard.Competences.Force, "or");

            totalCreatiumMin += creatiumRange.min;
            totalCreatiumMax += creatiumRange.max;
            totalOrMin += orRange.min;
            totalOrMax += orRange.max;
            totalCharisme += userCard.Competences.Charisme;
            totalEndTime.Add(CalculateActionEndTime(userCard.Competences.Exploration));
        }

        var averageCharisme = totalCharisme / userCards.Count;

        return new ActionEstimationResponse
        {
            EndDates = totalEndTime,
            ActionName = "explorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up exploration", "20%" },
                { "Creatium", $"{totalCreatiumMin} - {totalCreatiumMax}" },
                { "Or", $"{totalOrMin} - {totalOrMax}" },
                { "Chance de carte", averageCharisme * 10 + "%" }
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", (2 * userCards.Count).ToString() },
                { "Prend du temps à revenir", "15min"}
            },
            Error = errorMessages.ToString() // Retourne tous les messages d'erreur collectés
        };
    }


    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionExplorer actionExplorer)
            throw new AppException("Action not found", 404);

        if (action.UserCards.Count == 0)
        {
            Console.Error.WriteLine("Action has no user cards and is going to be deleted" + "\n" + action);
            context.Actions.Remove(action);
            return context.SaveChangesAsync();
        }
        
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
            
            // stats
            _statsService.OnRocketLaunched(user.Id);
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
        //return DateTime.Now.AddSeconds(10);
        return returning ? DateTime.Now.AddMinutes(15) : DateTime.Now.AddHours(5.5 - exploLevel * 0.5);
    }
}