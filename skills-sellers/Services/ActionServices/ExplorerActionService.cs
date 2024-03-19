using System.Text;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Registres;
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
        string notificationMessage;
        var userCard = action.UserCards.First();
        // get card ref
        context.Entry(userCard).Reference(uc => uc.Card).Load();

        if (actionExplorer is { needDecision: true, Decision: null })
        {
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Exploration bloquée",
                $"Voila un moment que votre carte {userCard.Card.Name} est bloquée sur une planète habitée ! \n" +
                "Vous devez lui indiquer les ordres à suivre pour qu'elle puisse revenir !",
                "onecard", userCard.CardId
            ), context);
            return Task.CompletedTask;
        }

        if (!actionExplorer.IsReturningToHome)
        {
            // notify user
            notificationMessage = $"Votre carte {userCard.Card.Name} est arrivée sur la planète {actionExplorer.PlanetName} !";
            
            // spacio registre update : define if action gonna need a decision or not
            var planeteHabite = WeaponUpdateInhabitedPart(context, user, actionExplorer);
            if (!planeteHabite) notificationMessage += "\r\n" + "Cette planète n'est pas habitée !";

            // Weapons Update
            WeaponUpdateExplorationPart(context, userCard, user);
            
            // update action to returning
            actionExplorer.IsReturningToHome = true;
            action.DueDate = CalculateActionEndTime(userCard.Competences.Exploration, true);
            
            // start timer for returning
            _ = _actionTaskService.StartNewTaskForAction(action);
        }
        else
        {
            // notify user
            notificationMessage = $"Votre carte {userCard.Card.Name} est revenue de l'exploration !\r\n";

            #region REWARDS
            
            // calculate loot possible
            var creatiumWin = _resourcesService.GetRandomValueForForceStat(userCard.Competences.Force, "creatium");
            var orWin = _resourcesService.GetRandomValueForForceStat(userCard.Competences.Force, "or");

            // if planet is inhabited, user can choose to pillage or ally
            switch (actionExplorer.Decision)
            {
                case ExplorationDecision.Pillage:
                    // ressources x4 and a card
                    creatiumWin *= 4;
                    orWin *= 4;
                    user.NbCardOpeningAvailable++;
                    notificationMessage += "Votre carte a pillé la planète ! Elle gagne une carte supplémentaire !\r\n";
                    var isPlanetHostile = WeaponUpdatePillagePart(context, user, actionExplorer);
                    if (isPlanetHostile)
                        notificationMessage += 
                            "Pillage - Hostile\r\n" +
                            "Votre carte a rencontré une planète hostile !\n" +
                            $"La planète {actionExplorer.PlanetName} est désormais dans votre registre ! \n";
                    break;
                case ExplorationDecision.Ally:
                    
                    var commerceDone = WeaponUpdateAllyPart(user, actionExplorer, userCard);
                    // notify user
                    if (commerceDone)
                        notificationMessage +=
                            "Alliance - Echec\r\n" +
                            "mais vous n'avez pas réussi à créer une route commerciale !\r\n" +
                            $"La planète {actionExplorer.PlanetName} est désormais dans votre registre !\r\n";
                    else
                        notificationMessage +=
                            "Alliance - Succès\r\n" +
                            $"Vous avez désormais une route commerciale avec {actionExplorer.PlanetName} !\n" +
                            "pour plus d'information, consultez votre registre !\n";
                    
                    creatiumWin /= 2; orWin /= 2;
                    break;
                case null:
                    break;
                default:
                    throw new AppException("Error when applying decision", 500);
            }

            // stats
            user.Creatium += creatiumWin;
            user.Or += orWin;
            user.Score += 5;
            
            _statsService.OnCreatiumMined(user.Id, creatiumWin);
            _statsService.OnOrMined(user.Id, orWin);
            
            // notify user
            notificationMessage += $"Votre carte {userCard.Card.Name} a gagné {creatiumWin} créatium et {orWin} or !\r\n";

            // chance to get a card based on charisme
            if (Randomizer.RandomPourcentageUp(userCard.Competences.Charisme * 10))
            {
                // notify user
                notificationMessage += $"Votre carte {userCard.Card.Name} a trouvé une nouvelle carte !\r\n";

                user.NbCardOpeningAvailable++;
            }
            else // stats
                _statsService.OnCardFailedCauseOfCharisme(user.Id);

            // chance to up exploration competence
            if (Randomizer.RandomPourcentageUp() && userCard.Competences.Exploration < 10)
            {
                userCard.Competences.Exploration += 1;
                // notify user
                notificationMessage += $"Votre carte {userCard.Card.Name} a gagné 1 point en exploration !\r\n";
            }

            #endregion
            
            // remove action if returning
            context.Actions.Remove(action);
            
            // stats
            _statsService.OnRocketLaunched(user.Id);
        }

        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Exploration",
            notificationMessage,
            "onecard", userCard.CardId
        ), context);
        
        return context.SaveChangesAsync();
    }

    private void WeaponUpdateExplorationPart(DataContext context, UserCard userCard, User user)
    {
        var (fightAppend, fightReport) = TryToEncounterAnOtherCardOnExploration(userCard, context, out var opponentCard, out var result);
        
        if (!fightAppend) return;
        string notificationMessage;
        string opponentNotificationMessage;

        switch (result)
        {
            case 1: // user win
                
                var stringReward = WarHelpers.GetRandomWarLoot(user);
                
                // score 
                user.Score += 50;

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
                
                // score
                opponent.Score += 10;
                
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
                $"Votre carte utilise son arme {userCard.UserWeapon.Weapon.Name} avec {userCard.UserWeapon.Power} de puissance et d'affinité {userCard.UserWeapon.Affinity}!\n";

        if (opponentCard?.UserWeapon != null)
            notificationMessage +=
                $"La carte adverse utilise son arme {opponentCard.UserWeapon.Weapon.Name} avec {opponentCard.UserWeapon.Power} de puissance et d'affinité {opponentCard.UserWeapon.Affinity}!\n";

        notificationMessage += "Plus d'informations dans le registre de combat!";
        
        // adding to registre if not already in
        context.Entry(user).Collection(u => u.Registres).Load();
        if (!user.Registres
            .Any(r => 
                r.Type == RegistreType.Player 
                && ((RegistrePlayer) r).RelatedPlayerId == opponentCard!.UserId))
        {
            var registreOfUser = WarHelpers.GeneratePlayerRegistre(user, opponentCard!.User, context.Cards);
            var registreOfOpponent = WarHelpers.GeneratePlayerRegistre(opponentCard.User, user, context.Cards);
        
            context.Registres.Add(registreOfUser);
            context.Registres.Add(registreOfOpponent);
        }

        // adding fightReport
        context.FightReports.Add(fightReport!);
        
        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Explorer - Combat",
            notificationMessage,
            "onecard", userCard.CardId
        ), context);
        
        // notify opponent
        _notificationService.SendNotificationToUser(opponentCard!.User, new NotificationRequest
        (
            "Explorer - Combat",
            opponentNotificationMessage,
            ""
        ), context);
    }

    private bool WeaponUpdateInhabitedPart(DataContext context, User user, ActionExplorer action)
    {
        // 2% que la planète soit hostile
        if (Randomizer.RandomPourcentageSeeded(action.PlanetName, 2))
        {
            // hostile
            context.Entry(user).Collection(u => u.Registres).Load();
            context.Entry(user).Reference(u => u.UserBatimentData).Load();
            
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Explorer - Hostile !",
                "Votre carte a rencontré une planète hostile !\n" +
                "La planète est désormais dans votre registre ! Elle risque de revenir...\n",
                ""
            ), context);
            
            var registre = WarHelpers.GenerateHostileRegistre(user, action.PlanetName);
            user.Registres.Add(registre);
            return true;
        }

        // 15% de chance que la planete soit habitée
        if (!Randomizer.RandomPourcentageSeeded(action.PlanetName, 15))
        { // non habitée
            // adding to registre as neutral
            var registre = WarHelpers.GenerateNeutralRegistre(user, action.PlanetName);
            user.Registres.Add(registre);
            return false;
        } 
        
        // a besoin d'une décision
        action.needDecision = true;

        var userCard = action.UserCards.First();
        context.Entry(userCard).Reference(uc => uc.Card).Load();
            
        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Explorer - Diplomatie",
            $"Votre carte {userCard.Card.Name} a rencontré une planète habitée ! Elle ne peut pas revenir pour le moment ! \n" +
            "Vous devez lui indiquer les ordres à suivre pour qu'elle puisse revenir ! \n",
            "onecard", userCard.CardId
        ), context);
        return true;
    }

    private bool WeaponUpdatePillagePart(DataContext context, User user, ActionExplorer action)
    {
        Registre registre;
        
        // 60% de chance que la planète deviennent hostile
        if (!Randomizer.RandomPourcentageSeeded(action.PlanetName, 60))
        {
            // add registre as Neutral
            registre = WarHelpers.GenerateNeutralRegistre(user, action.PlanetName);
            user.Registres.Add(registre);
            user.Score += 20;

            return false; // non hostile
        }
        
        // hostile
        context.Entry(user).Collection(u => u.Registres).Load();
        context.Entry(user).Reference(u => u.UserBatimentData).Load();
        
        registre = WarHelpers.GenerateHostileRegistre(user, action.PlanetName);
        
        user.Registres.Add(registre);

        return true;
    }

    private bool WeaponUpdateAllyPart(User user, ActionExplorer action, UserCard userCard)
    {
        Registre registre;
        
        // 10 % de créer une route commerciale
        if (!Randomizer.RandomPourcentageSeeded(action.PlanetName, 10)) // non
        {

            // add registre as Neutral
            registre = WarHelpers.GenerateNeutralRegistre(user, action.PlanetName);
            user.Registres.Add(registre);
            return false;
        }

        // oui
        // add registre as Friendly
        registre = WarHelpers.GenerateFriendlyRegistre(
            user, 
            action.PlanetName, 
            userCard.ToResponse().Power);
        user.Registres.Add(registre);
        user.Score += 10;
        return true;
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
    private (bool result, FightReport? report) TryToEncounterAnOtherCardOnExploration(UserCard userCard, DataContext context, out UserCard? randomCard, out int result)
    {
        try
        {
            randomCard = null;
            result = 0;
            
            // 5% de chance de rencontrer une autre carte
            if (!Randomizer.RandomPourcentageUp(5))
                return (false, null);
            
            // get all cards in exploration (except the current one and all user's cards)
            var cardsInExploration = context.UserCards
                .Where(uc => uc.Action is ActionExplorer)
                .Where(uc => uc.UserId != userCard.UserId)
                .Where(uc => ((ActionExplorer)uc.Action!).IsReturningToHome == false)
                .Include(uc => uc.Competences)
                .Include(uc => uc.UserWeapon)
                .ThenInclude(uw => uw.Weapon)
                .Include(uc => uc.Card)
                .Include(uc => uc.User)
                .ToList();

            if (cardsInExploration.Count == 0)
                return (false, null);
            
            // get random card
            randomCard = cardsInExploration[Randomizer.RandomInt(0, cardsInExploration.Count)];
            
            // fight
            var fightResult = WarHelpers.Fight(userCard, randomCard);
            
            // get description
            var fightReport = WarHelpers.GetFightDescription(userCard, randomCard);
            
            // if draw, no one win
            result = fightResult.result;

            return (true, fightReport);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}