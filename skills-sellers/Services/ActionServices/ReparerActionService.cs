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

public class ReparerActionService : IActionService<ActionReparer>
{
    private DataContext _context;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    public ReparerActionService(
        DataContext context,
        IServiceProvider serviceProvider, 
        INotificationService notificationService)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (user.StatRepairedObjectMachine != -1)
            throw new AppException("Vous avez déjà réparé la machine !", 400);
        
        if (userCards.Count < 1)
            throw new AppException("Vous devez jouer une carte minimum !", 400);
        
        // cards already in action ?
        if (userCards.Any(c => c.Action != null))
            throw new AppException("Une de vos cartes est déjà en action !", 400);
        
        return (true, "");
    }

    public ActionReparer? GetAction(UserCard userCard)
    {
        return IncludeGetActionsReparer()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                           && uc.UserId == userCard.UserId));
    }

    public List<ActionReparer> GetActions()
    {
        return IncludeGetActionsReparer().ToList();
    }

    public async Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible ! " + validation.why, 400);
        
        // set cards in action
        var action = new ActionReparer
        {
            UserCards = userCards,
            DueDate = CalculateActionEndTime(),
            RepairChances = model.RepairChances,
            User = user
        };
        
        // actualise bdd
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
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible ! " + validation.why, 400);
        
        // calculate action end time and resources
        var endTime = CalculateActionEndTime();
        
        return new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "ameliorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Machine accessible", "" }
            },
            Couts = new Dictionary<string, string>
            {
            },
            Error = !validation.valid ? "Impossible de réparer la machine : " + validation.why : null
        };
    }

    public async Task EndAction(int actionId)
    {
        // get data
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);

        var user = action.User;
        
        // set repaired depending on the chances
        var random = new Random();
        var chances = random.Next(0, 100);
        if (chances < action.RepairChances)
        {
            user.StatRepairedObjectMachine = 0;
            await _context.SaveChangesAsync();
            
            // notify user
            await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Réparation terminée", 
                    $"La réparation de la machine est terminée ! Vous pouvez maintenant l'utiliser !"), 
                _context);
        }
        else await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Réparation échouée",
                    $"La réparation de la machine a échouée ! Vous pouvez retenter votre chance !"),
                _context);
        
        // remove action
        _context.Actions.Remove(action);
        
        await _context.SaveChangesAsync();
    }

    public Task DeleteAction(User user, int actionId)
    {
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);
        
        _context.Actions.Remove(action);
        
        // refund
        user.StatRepairedObjectMachine = -1;

        // cancel task
        if (TaskCancellations.TryGetValue(action.Id, out var cts))
            cts.Cancel();
        
        return _context.SaveChangesAsync();
    }

    public Task RegisterNewTaskForActionAsync(ActionReparer action, User user)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.TryAdd(action.Id, cts);
        
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();

        return StartTaskForActionAsync(action, cts.Token);
    }
    
    private async Task StartTaskForActionAsync(ActionReparer action, CancellationToken cancellationToken)
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

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    // Helpers
    
    private IIncludableQueryable<ActionReparer,Object> IncludeGetActionsReparer()
    {
        return _context.Actions
            .OfType<ActionReparer>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    }
    
    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddHours(1);
    }
}