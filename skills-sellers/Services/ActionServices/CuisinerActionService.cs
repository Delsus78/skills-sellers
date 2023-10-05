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
    
    public CuisinerActionService(
        DataContext context,
        IUserBatimentsService userBatimentsService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
        _serviceProvider = serviceProvider;
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

    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards)
    {
        // une seule carte pour cuisiner
        if (userCards.Count != 1)
            return (false, "Une seule carte pour cuisiner");
        var userCard = userCards.First();
        
        // Carte déjà en action
        if (GetAction(userCard) != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "cuisine"))
            return (false, "Batiment déjà plein, attendez demain !");
        
        // Stats et ressources suffisantes ?
        // Cuisiner ne nécessite pas de ressources ni de minimum de stats
        
        return (true, "");
    }

    #endregion

    #region Starters
    
    public async Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards);
        if (!validation.valid)
            throw new AppException("Impossible de cuisiner : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();
        
        // Random plat
        var randomPlat = FoodRandomizer.RandomPlat();
        
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
        var validation = CanExecuteAction(user, userCards);
        if (!validation.valid)
            throw new AppException("Impossible de cuisiner : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "cuisiner",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "nourriture", (userCards.First().Competences.Cuisine - 1).ToString() },
                { "Up cuisine", "20%" }
            }
        };
        
        // return response
        return action;
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
        var amount = userCard.Competences.Cuisine - 1;
        user.Nourriture += amount;

        // chance to up cuisine competence
        // - 20% de chance de up
        if (FoodRandomizer.RandomCuisineUp() && userCard.Competences.Cuisine < 10)
        {
            userCard.Competences.Cuisine += 1;
            // TODO notify user
            
            _context.UserCards.Update(userCard);
        }
        
        _context.Users.Update(user);
        #endregion
        
        
        // remove action
        _context.Actions.Remove(action);

        // notify user
        // TODO notify user

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
    
    private IIncludableQueryable<ActionCuisiner,Object> IncludeGetActionsCuisiner(DataContext context)
    {
        return context.Actions
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