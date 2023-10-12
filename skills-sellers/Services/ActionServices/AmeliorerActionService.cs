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

public class AmeliorerActionService : IActionService<ActionAmeliorer>
{
    private DataContext _context;
    private readonly INotificationService _notificationService;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IStatsService _statsService;

    public AmeliorerActionService(
        DataContext context,
        IUserBatimentsService userBatimentsService,
        IServiceProvider serviceProvider, 
        INotificationService notificationService, 
        IStatsService statsService)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
        _statsService = statsService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (model?.BatimentToUpgrade == null)
            return (false, "Batiment à améliorer non spécifié");

        // une seule carte pour améliorer
        if (userCards.Count < 1)
            return (false, "Une carte ou plus est nécessaire pour améliorer un bâtiment !");

        // une des carte est déjà en action
        if (userCards.Any(uc => uc.Action != null))
            return (false, "Une des cartes est déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "laboratoire"))
            return (false, "Batiment déjà plein");
        
        // Stats et ressources suffisantes ?
        if (user.Nourriture < userCards.Count)
            return (false, "Pas assez de nourritures");
        
        var level = GetLevelOfUserBat(user.UserBatimentData, model);

        (int creatiumPrice, int intelPrice, int forcePrice) = _userBatimentsService.GetBatimentPrices(level);
        
        if (user.Creatium < creatiumPrice)
            return (false, "Pas assez de créatium => " + user.Creatium + " < " + creatiumPrice);
        
        var intelTotal = userCards.Sum(uc => uc.Competences.Intelligence);
        var forceTotal = userCards.Sum(uc => uc.Competences.Force);
        if (intelTotal < intelPrice)
            return (false, "Pas assez d'intelligence => " + intelTotal + " < " + intelPrice);
        
        if (forceTotal < forcePrice)
            return (false, "Pas assez de force => " + forceTotal + " < " + forcePrice);

        return (true, "");
    }

    public ActionAmeliorer? GetAction(UserCard userCard)
    {
        return IncludeGetActionsAmeliorer()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                           && uc.UserId == userCard.UserId));
    }

    public List<ActionAmeliorer> GetActions()
    {
        return IncludeGetActionsAmeliorer().ToList();
    }

    public async Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible d'améliorer le bâtiment : " + validation.why, 400);
        
        // get user bat level
        var level = GetLevelOfUserBat(user.UserBatimentData, model);
        
        // calculate action end time
        var endTime = CalculateActionEndTime(level);

        var action = new ActionAmeliorer
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            BatimentToUpgrade = model.BatimentToUpgrade
        };
        
        // actualise bdd
        await _context.Actions.AddAsync(action);
        
        // consume resources
        var (creatiumPrice, _, _) = _userBatimentsService.GetBatimentPrices(level);
        
        user.Nourriture -= userCards.Count;
        user.Creatium -= creatiumPrice;
        
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

        // validate action
        var validation = CanExecuteAction(user, userCards, model);

        // get user bat level
        var level = GetLevelOfUserBat(user.UserBatimentData, model);
        
        // calculate action end time and resources
        var endTime = CalculateActionEndTime(level);
        var (creatiumPrice, _, _) = _userBatimentsService.GetBatimentPrices(level);

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "ameliorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up intel", level + " fois random" },
                { "Up lvl bâtiment", "+1"}
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", userCards.Count.ToString() },
                { "créatium", creatiumPrice.ToString() }
            },
            Error = !validation.valid ? "Impossible d'ameliorer le batiment : " + validation.why : null
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

        // sort cards by intel and remove all that got are max 10 intel
        var userCards = action.UserCards
            .OrderBy(uc => uc.Competences.Intelligence)  // Change OrderByDescending to OrderBy to start with the card with the lowest intel
            .Where(uc => uc.Competences.Intelligence < 10)
            .ToList();
        
        var userBatimentData = _userBatimentsService.GetOrCreateUserBatimentData(action.User, _context);

        var niveauIntelADonner = GetLevelOfUserBat(userBatimentData, new ActionRequest { BatimentToUpgrade = action.BatimentToUpgrade });

        var cardNameForIntelUp = new Dictionary<string, int>();
        
        while (niveauIntelADonner > 0 && userCards.Any(uc => uc.Competences.Intelligence < 10))
        {
            foreach (var card in userCards
                         .Where(card => niveauIntelADonner > 0 &&
                                        card.Competences.Intelligence < 10))
            {
                card.Competences.Intelligence++;
                niveauIntelADonner--;
                
                // add card name to dictionary for notification
                if (cardNameForIntelUp.ContainsKey(card.Card.Name))
                    cardNameForIntelUp[card.Card.Name]++;
                else
                    cardNameForIntelUp.Add(card.Card.Name, 1);
            }
        }
        
        // notify user
        _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
            "Amélioration terminée", 
            $"Les cartes suivantes ont gagné des points d'intelligence : {string.Join(", ", cardNameForIntelUp.Select(kvp => $"{kvp.Key} (+{kvp.Value})"))}"), 
            _context);

        // up batiment level
        switch (action.BatimentToUpgrade)
        {
            case "cuisine":
                userBatimentData.CuisineLevel++;
                break;
            case "salledesport":
                userBatimentData.SalleSportLevel++;
                break;
            case "spatioport":
                userBatimentData.SpatioPortLevel++;
                break;
            default:
                throw new AppException("Bâtiment non reconnu", 400);
        }
        
        // stats
        _statsService.OnBuildingsUpgraded(action.User.Id);
        
        // remove action
        _context.Actions.Remove(action);

        // notify user
        _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
            "Amélioration terminée", 
            $"Votre bâtiment {action.BatimentToUpgrade} a été amélioré !"), 
            _context);

        return _context.SaveChangesAsync();
    }

    public Task RegisterNewTaskForActionAsync(ActionAmeliorer action, User user)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.TryAdd(action.Id, cts);
        
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();

        return StartTaskForActionAsync(action, cts.Token);
    }
    
    private async Task StartTaskForActionAsync(ActionAmeliorer action, CancellationToken cancellationToken)
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
    
    private IIncludableQueryable<ActionAmeliorer,Object> IncludeGetActionsAmeliorer()
    {
        return _context.Actions
            .OfType<ActionAmeliorer>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
    
    private DateTime CalculateActionEndTime(int level)
    {
        return DateTime.Now.AddHours(12 * level);
    }
    
    private static int GetLevelOfUserBat(UserBatimentData batimentData, ActionRequest model)
    {
        // get batiment requested level (BatimentToUpgrade)
        var level = model.BatimentToUpgrade switch
        {
            "cuisine" => batimentData.CuisineLevel,
            "salledesport" => batimentData.SalleSportLevel,
            "spatioport" => batimentData.SpatioPortLevel,
            _ => throw new AppException("Bâtiment non reconnu", 400)
        };
        return level;
    }
}