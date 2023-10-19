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

public class CuisinerActionService : IActionService<ActionCuisiner>
{
    private DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notificationService;
    private readonly IStatsService _statsService;
    
    public CuisinerActionService(
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

    public ActionCuisiner? GetAction(UserCard userCard)
    {
        return IncludeGetActionsCuisiner()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                           && uc.UserId == userCard.UserId));
    }
    
    public List<ActionCuisiner> GetActions()
    {
        return IncludeGetActionsCuisiner().ToList();
    }
    
    #region Validator

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // une seule carte pour cuisiner
        if (userCards.Count != 1)
            return (false, "Une seule carte pour cuisiner");
        var userCard = userCards.First();
        
        // Carte déjà en action
        if (userCard.Action != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "cuisine"))
            return (false, "Batiment déjà plein, attendez demain !");

        return (true, "");
    }

    #endregion

    #region Starters
    
    public async Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
            throw new AppException("Impossible de cuisiner : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();
        
        // Random plat
        var randomPlat = Randomizer.RandomPlat();
        
        var action = new ActionCuisiner
        {
            UserCards = userCards,
            DueDate = endTime,
            Plat = randomPlat,
            User = user
        };
        
        // actualise bdd and nb cuisine used today
        user.UserBatimentData.NbCuisineUsedToday += 1;
        
        await _context.Actions.AddAsync(action);
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
        var validation = CanExecuteAction(user, userCards, null);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "cuisiner",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "nourriture", (userCards.First().Competences.Cuisine).ToString() },
                { "Up cuisine", "20%" }
            },
            Error = !validation.valid ? "Impossible de cuisiner : " + validation.why : null
        };
        
        // return response
        return action;
    }

    public Task DeleteAction(User user, int actionId)
    {
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);
        
        _context.Actions.Remove(action);
        
        // actualise bdd and nb cuisine used today
        user.UserBatimentData.NbCuisineUsedToday -= 1;
        
        // cancel task
        if (TaskCancellations.TryGetValue(action.Id, out var cts))
            cts.Cancel();
        
        return _context.SaveChangesAsync();
    }
    
    #endregion
    
    #region TaskService
    
    public Task RegisterNewTaskForActionAsync(ActionCuisiner action, User user)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.TryAdd(action.Id, cts);
        
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();

        return StartTaskForActionAsync(action, cts.Token);
    }
    private async Task StartTaskForActionAsync(
        ActionCuisiner action,
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
            }
        }
        else
        {
            // La date d'échéance est déjà passée
            await EndAction(action.Id);
        }
        
        TaskCancellations.TryRemove(action.Id, out _);
    }

    public Task EndAction(int actionId)
    {
        // get data
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);

        var user = action.User;
        
        // only one card
        var userCard = action.UserCards.First();

        #region REWARDS

        // give nourriture
        var amount = userCard.Competences.Cuisine;
        user.Nourriture += amount;
        
        // stats
        _statsService.OnMealCooked(user.Id);

        // chance to up cuisine competence
        // - 20% de chance de up
        if (Randomizer.RandomPourcentageUp() && userCard.Competences.Cuisine < 10)
        {
            userCard.Competences.Cuisine += 1;
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Compétence cuisine",
                $"Votre carte {userCard.Card.Name} a gagné 1 point de compétence en cuisine !"
            ), _context);
        }
        
        #endregion
        
        
        // remove action
        _context.Actions.Remove(action);

        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Cuisiner",
            $"Votre carte {userCard.Card.Name} a cuisiné {amount} nourriture avec son plat {action.Plat} !"
        ), _context);
        
        return _context.SaveChangesAsync();
    }
    
    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();

    #endregion
    
    // Helpers
    
    private IIncludableQueryable<ActionCuisiner,Object> IncludeGetActionsCuisiner()
    {
        return _context.Actions
            .OfType<ActionCuisiner>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    }

    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddMinutes(30);
    }
}