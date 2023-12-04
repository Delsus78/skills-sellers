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

        for (var index = 0; index < userCards.Count; index++)
        {
            var userCard = userCards[index];
            // validate action
            var validation = CanExecuteAction(user, new List<UserCard> { userCard }, null);
            
            if (!validation.valid)
                throw new AppException("Impossible de partir en exploration : " + validation.why, 400);

            // calculate action end time
            var endTime = CalculateActionEndTime(userCard.Competences.Exploration).AddSeconds(-index);

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

            // // chance to up exploration competence
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
            
            // Weapons Update
            WeaponUpdateExplorationPart(context, userCard, user);

            #endregion
            
            // update action to returning
            actionExplorer.IsReturningToHome = true;
            action.DueDate = CalculateActionEndTime(userCard.Competences.Exploration, true);
            
            // start timer for returning
            _ = _actionTaskService.StartNewTaskForAction(action);
        }

        return context.SaveChangesAsync();
    }

    private void WeaponUpdateExplorationPart(DataContext context, UserCard userCard, User user)
    {
        if (!TryToEncounterAnOtherCardOnExploration(userCard, context, out var opponentCard, out var result)) return;
        var notificationMessage = "";
        var opponentNotificationMessage = "";

        switch (result)
        {
            case 1: // user win
                
                var stringReward = WarHelpers.GetRandomWarLoot(user);

                notificationMessage =
                    $"Votre carte {userCard.Card.Name} a rencontré une autre carte et a gagné !\n" +
                    $"Elle s'est battue contre {opponentCard?.Card.Name} de {opponentCard?.User.Pseudo} ! \n" +
                    $"Elle a gagné {stringReward} ! \n";
                
                opponentNotificationMessage =
                    $"Votre carte {opponentCard?.Card.Name} a rencontré une autre carte et a perdu !\n" +
                    $"Elle s'est battue contre {userCard.Card.Name} de {user.Pseudo} ! \n";
                break;
            case -1: // opponent win
                var opponent = opponentCard!.User;
                var stringRewardOpponent = WarHelpers.GetRandomWarLoot(opponent, true);
                
                notificationMessage =
                    $"Votre carte {userCard.Card.Name} a rencontré une autre carte et a perdu !\n" +
                    $"Elle s'est battue contre {opponentCard?.Card.Name} de {opponentCard?.User.Pseudo} ! \n";

                opponentNotificationMessage =
                    $"Votre carte {opponentCard?.Card.Name} a rencontré une autre carte et a gagné !\n" +
                    $"Elle s'est battue contre {userCard.Card.Name} de {user.Pseudo} ! \n" +
                    $"Elle a gagné {stringRewardOpponent} ! \n";
                
                break;
            default:
                notificationMessage =
                    $"Votre carte {userCard.Card.Name} a rencontré une autre carte et a fait match nul !\n" +
                    $"Elle s'est battue contre {opponentCard?.Card.Name} de {opponentCard?.User.Pseudo} ! \n";

                opponentNotificationMessage =
                    $"Votre carte {opponentCard?.Card.Name} a rencontré une autre carte et a fait match nul !\n" +
                    $"Elle s'est battue contre {userCard.Card.Name} de {user.Pseudo} ! \n";

                break;
        }

        notificationMessage +=
            $"COMPTE RENDU DU COMBAT : \n" +
            $"Votre carte a {userCard.ToResponse().Power} de puissance\n" +
            $"La carte adverse a {opponentCard?.ToResponse().Power} de puissance.\n";

        if (userCard.UserWeapon != null)
            notificationMessage +=
                $"Votre carte utilise son arme {userCard.UserWeapon.Weapon.Name} avec {userCard.UserWeapon.Power} avec l'affinité {userCard.UserWeapon.Affinity}!\n";

        if (opponentCard?.UserWeapon != null)
            notificationMessage +=
                $"La carte adverse utilise son arme {opponentCard.UserWeapon.Weapon.Name} avec {opponentCard.UserWeapon.Power} avec l'affinité {opponentCard.UserWeapon.Affinity}!\n";

        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Explorer - Combat",
            notificationMessage
        ), context);
        
        // notify opponent
        _notificationService.SendNotificationToUser(opponentCard!.User, new NotificationRequest
        (
            "Explorer - Combat",
            opponentNotificationMessage
        ), context);
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
    
    // Weapons Update
    private bool TryToEncounterAnOtherCardOnExploration(UserCard userCard, DataContext context, out UserCard? randomCard, out int result)
    {
        randomCard = null;
        result = 0;
        
        // 10% de chance de rencontrer une autre carte
        if (!Randomizer.RandomPourcentageUp(10))
            return false;
        
        // get all cards in exploration (except the current one and all user's cards)
        var cardsInExploration = context.UserCards
            .Where(uc => uc.Action is ActionExplorer)
            .Where(uc => uc.UserId != userCard.UserId)
            .Include(uc => uc.Competences)
            .Include(uc => uc.UserWeapon)
            .ThenInclude(uw => uw.Weapon)
            .Include(uc => uc.Card)
            .Include(uc => uc.User)
            .ToList();
        
        // get random card
        randomCard = cardsInExploration[Randomizer.RandomInt(0, cardsInExploration.Count - 1)];
        
        // fight
        var fightResult = WarHelpers.Fight(userCard, randomCard);
        
        // if draw, no one win
        result = fightResult.result;

        return true;
    }
}