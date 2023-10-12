using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services.ActionServices;

public class ExplorerActionService : IActionService<ActionExplorer>
{
    private DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IResourcesService _resourcesService;
    private readonly INotificationService _notificationService;
    private readonly IStatsService _statsService;
    
    public ExplorerActionService(
        DataContext context,
        IUserBatimentsService userBatimentsService,
        IServiceProvider serviceProvider,
        IResourcesService resourcesService, INotificationService notificationService, IStatsService statsService)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
        _serviceProvider = serviceProvider;
        _resourcesService = resourcesService;
        _notificationService = notificationService;
        _statsService = statsService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour explorer
        if (userCards.Count != 1)
            return (false, "Une carte seule est nécessaire pour explorer !");
        var userCard = userCards.First();

        // Carte déjà en action
        if (GetAction(userCard) != null)
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

    public ActionExplorer? GetAction(UserCard userCard)
    {
        return IncludeGetActionsExplorer()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                           && uc.UserId == userCard.UserId));
    }

    public List<ActionExplorer> GetActions()
    {
        return IncludeGetActionsExplorer().ToList();
    }

    public async Task<ActionResponse> StartAction(User user, ActionRequest model)
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
        await _context.Actions.AddAsync(action);
        
        // stats
        _statsService.OnRocketLaunched(user.Id);
        
        // consume resources
        user.Nourriture -= 2;
        
        await _context.SaveChangesAsync();
        
        // start timer
        _ = RegisterNewTaskForActionAsync(action, user)
            .ContinueWith(t =>
            {
                if (t is { IsFaulted: true, Exception: not null })
                {
                    Console.Error.WriteLine(t.Exception);
                }
            });
        
        // return response
        return action.ToResponse();
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

    public Task EndAction(int actionId)
    {
        // get data
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);

        var user = action.User;
        
        var userCard = action.UserCards.First();

        if (action.IsReturningToHome)
            // remove action if returning
            _context.Actions.Remove(action);
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
            ), _context);

            // chance to get a card based on charisme
            if (Randomizer.RandomPourcentageUp(userCard.Competences.Charisme * 10))
            {
                // notify user
                _notificationService.SendNotificationToUser(user, new NotificationRequest
                (
                    "Explorer",
                    $"Votre carte {userCard.Card.Name} a trouvé une nouvelle carte !"
                ), _context);

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
                ), _context);

                _context.UserCards.Update(userCard);
            }

            _context.Users.Update(user);
            
            #endregion
            
            // update action to returning
            action.IsReturningToHome = true;
            action.DueDate = CalculateActionEndTime(userCard.Competences.Exploration, true);
            _context.Actions.Update(action);
            
            // start timer for returning
            var cts = new CancellationTokenSource();
            TaskCancellations.TryAdd(action.Id, cts);

            _ = StartTaskForActionAsync(action, cts.Token);
        }

        return _context.SaveChangesAsync();
    }

    public Task RegisterNewTaskForActionAsync(ActionExplorer action, User user)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.TryAdd(action.Id, cts);
        
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();

        return StartTaskForActionAsync(action, cts.Token);
    }

    private async Task StartTaskForActionAsync(
        ActionExplorer action,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var delay = action.DueDate - now;
        
        if (delay.TotalMilliseconds > 0)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await EndAction(action.Id);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"Task {action.Id} cancelled");
                _context.Actions.Remove(action);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // La date d'échéance est déjà passée
            await EndAction(action.Id);
        }
        
        TaskCancellations.TryRemove(action.Id, out _);
    }

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    // Helpers
    
    private IIncludableQueryable<ActionExplorer,Object> IncludeGetActionsExplorer()
    {
        return _context.Actions
            .OfType<ActionExplorer>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    }
    
    private DateTime CalculateActionEndTime(int exploLevel, bool returning = false)
    {
        // l’exploration prendra 5h30 - le niveau x 30 minutes 
        return returning ? DateTime.Now.AddMinutes(15) : DateTime.Now.AddHours(5.5 - exploLevel * 0.5);
    }
}