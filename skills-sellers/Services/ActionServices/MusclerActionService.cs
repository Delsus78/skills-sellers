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

public class MusclerActionService : IActionService<ActionMuscler>
{
    private DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notificationService;
    
    public MusclerActionService(
        DataContext context,
        IUserBatimentsService userBatimentsService,
        IServiceProvider serviceProvider, INotificationService notificationService)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
    }
    
    public override (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest model)
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

    public override ActionMuscler? GetAction(UserCard userCard)
    {
        return IncludeGetActionsMuscler()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                           && uc.UserId == userCard.UserId));
    }

    public override List<ActionMuscler> GetActions()
    {
        return IncludeGetActionsMuscler().ToList();
    }

    public override async Task<ActionResponse> StartAction(User user, ActionRequest? model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
            throw new AppException("Impossible de se muscler : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime(userCards.First().Competences.Force  + 1);
        
        // random muscle
        var muscle = Randomizer.RandomMuscle();
        
        var action = new ActionMuscler
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            Muscle = muscle
        };
        
        // actualise bdd
        await _context.Actions.AddAsync(action);
        
        // consume resources
        user.Nourriture -= 1;
        
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

    public override ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);

        // calculate action end time
        var endTime = CalculateActionEndTime(userCards.First().Competences.Force  + 1);

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "muscler",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up force", "100%" }
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", "1" }
            },
            Error = !validation.valid ? "Impossible d'aller pousser à la salle : " + validation.why : null
        };
        
        // return response
        return action;
    }

    public override Task EndAction(int actionId)
    {
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
        // get data
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);

        // only one card
        var userCard = action.UserCards.First();

        // up force competence
        
        userCard.Competences.Force += 1;

        _context.UserCards.Update(userCard);

        // remove action
        _context.Actions.Remove(action);

        // notify user
        _notificationService.SendNotificationToUser(userCard.User, new NotificationRequest
        (
            "Salle de sport",
            $"Votre carte {userCard.Card.Name} a gagné 1 point de force !"
        ), _context);

        return _context.SaveChangesAsync();
    }

    public override Task DeleteAction(User user, int actionId)
    {
        var action = GetActions().FirstOrDefault(a => a.Id == actionId);
        if (action == null)
            throw new AppException("Action not found", 404);
        
        _context.Actions.Remove(action);
        
        // refund resources
        user.Nourriture += 1;
        
        
        // cancel task
        if (TaskCancellations.TryGetValue(action.Id, out var cts))
            cts.Cancel();
        
        return _context.SaveChangesAsync();
    }
    
    public override Task RegisterNewTaskForActionAsync(ActionMuscler action, User user)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.TryAdd(action.Id, cts);
        
        _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();

        return StartTaskForActionAsync(action, cts.Token);
    }

    // Helpers
    
    private IIncludableQueryable<ActionMuscler,Object> IncludeGetActionsMuscler()
    {
        return _context.Actions
            .OfType<ActionMuscler>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
    
    private DateTime CalculateActionEndTime(int forceLevelToUp)
    {
        return DateTime.Now.AddHours(forceLevelToUp);
    }
}