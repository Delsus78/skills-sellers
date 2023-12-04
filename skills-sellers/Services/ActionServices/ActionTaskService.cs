using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public interface IActionTaskService
{
    Task<List<ActionResponse>> CreateNewActionAsync(int userId, ActionRequest model);
    Task StartNewTaskForAction(Action action);
    Task RestartAllActionsAsync();
    Task StopAllActionsAsync();
    Task DeleteActionAsync(int userId, int actionId);
    ActionEstimationResponse EstimateAction(int userId, ActionRequest model);
}
public class ActionTaskService : IActionTaskService
{
    private readonly IServiceProvider _serviceProvider;
    private ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    public ActionTaskService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<List<ActionResponse>> CreateNewActionAsync(int userId, ActionRequest model)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var service = ActionServiceResolver.Resolve(model.ActionName.ToLower(), scope);
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = GetUser(userId, context);

        // start action
        var responseActions = await service.StartAction(user, model, context, _serviceProvider);
        
        // start timer
        foreach (var responseAction in responseActions)
            _ = StartNewTaskForAction(responseAction);

        return responseActions.Select(a => a.ToResponse()).ToList();
    }
    
    public async Task RestartAllActionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var ongoingActions = await context.Actions
            .Include(a => a.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences)
            .ToListAsync();

        foreach (var action in ongoingActions)
        {
            Console.Out.WriteLine("Reprise de l'action " + action.Id + " : " + action.GetType().Name + " pour " + action.User.Pseudo);
            _ = RegisterNewTaskForActionAsync(action).ContinueWith(t =>
            {
                if (t is { IsFaulted: true, Exception: not null })
                {
                    Console.Error.WriteLine(t.Exception);
                }
            });
        }
    }
    
    public async Task StopAllActionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var ongoingActions = await context.Actions
            .Include(a => a.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences)
            .ToListAsync();

        foreach (var action in ongoingActions)
        {
            if (TaskCancellations.TryGetValue(action.Id, out var cts))
                cts.Cancel();
        }
    }
    
    public Task StartNewTaskForAction(Action action)
    {
        _ = RegisterNewTaskForActionAsync(action).ContinueWith(t =>
        {
            if (t is { IsFaulted: true, Exception: not null })
            {
                Console.Error.WriteLine(t.Exception);
            }
        });
        
        return Task.CompletedTask;
    }

    public ActionEstimationResponse EstimateAction(int userId, ActionRequest model)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var service = ActionServiceResolver.Resolve(model.ActionName, scope);
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var user = GetUser(userId, context);
        
        return service.EstimateAction(user, model);
    }
    
    public async Task DeleteActionAsync(int userId, int actionId)
    {
        await DispatchToCorrectDeleteActionService(actionId);

        // cancel task
        if (TaskCancellations.TryGetValue(actionId, out var cts))
            cts.Cancel();
    }

    #region PRIVATES
    private Task RegisterNewTaskForActionAsync(Action action)
    {
        var cts = new CancellationTokenSource();
        TaskCancellations.AddOrUpdate(action.Id, cts, (_, _) => cts);

        return StartTaskForActionAsync(action, cts.Token);
    }
    
    private async Task StartTaskForActionAsync(Action action, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var delay = action.DueDate - now;
        
        Console.WriteLine($"Task {action.Id} started at {now} with delay {delay.TotalMilliseconds}ms");
        
        if (delay.TotalMilliseconds > 0)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await DispatchToCorrectEndActionService(action.Id);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"Task {action.Id} cancelled");
            }
            catch (AppException e)
            {
                Console.WriteLine($"Task {action.Id} errored with : " + $"[{e.ErrorCode}] - {e.Message}" + action);
            }
        }
        else
        {
            try
            {
                // La date d'échéance est déjà passée
                await DispatchToCorrectEndActionService(action.Id);
            }
            catch (AppException e)
            {
                Console.WriteLine($"Task {action.Id} errored with : " + $"[{e.ErrorCode}] - {e.Message}" + "\n" + action);
            }
        }

        Console.Out.WriteLine("Action " + action.Id + " is removed : " + TaskCancellations.TryRemove(action.Id, out _));
    }

    private async Task DispatchToCorrectService(int actionId, Func<IActionService, Action, DataContext, IServiceProvider, Task> actionFunc)
    {
        if (actionId <= 0)
        {
            throw new ArgumentException("L'ID de l'action doit être supérieur à zéro.", nameof(actionId));
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var actionEntity = await context.Actions
            .Where(a => a.Id == actionId)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences)
            .FirstOrDefaultAsync();

        if (actionEntity == null)
            throw new AppException("Action non trouvée", 404);

        var service = ActionServiceResolver.Resolve(actionEntity, scope);
        await actionFunc(service, actionEntity, context, _serviceProvider);
        
        await context.DisposeAsync();
    }

    // TODO : refactor to add a queue system to avoid multiple actions at the same time
    private Task DispatchToCorrectEndActionService(int actionId) =>
        DispatchToCorrectService(actionId,
            (service, action, context, serviceProvider) 
                => service.EndAction(action, context, serviceProvider));
    
    private Task DispatchToCorrectDeleteActionService(int actionId) =>
        DispatchToCorrectService(actionId, 
            (service, action, context, serviceProvider) 
            => service.DeleteAction(action, context, serviceProvider));

    private User GetUser(int id, DataContext context)
    {
        var user = context.Users
            .Where(u => u.Id == id)
            .SelectUserDetails()
            .FirstOrDefault();
        
        if (user == null)
            throw new AppException("Utilisateur non trouvé", 404);
        
        return user;
    }
    
    #endregion
}

// Factory ou stratégie pour résoudre le service d'action approprié
public class ActionServiceResolver
{
    public static IActionService Resolve(Action action, IServiceScope scope)
    {
        return action switch
        {
            ActionMuscler _ => scope.ServiceProvider.GetRequiredService<MusclerActionService>(),
            ActionCuisiner _ => scope.ServiceProvider.GetRequiredService<CuisinerActionService>(),
            ActionReparer _ => scope.ServiceProvider.GetRequiredService<ReparerActionService>(),
            ActionAmeliorer _ => scope.ServiceProvider.GetRequiredService<AmeliorerActionService>(),
            ActionExplorer _ => scope.ServiceProvider.GetRequiredService<ExplorerActionService>(),
            ActionSatellite _ => scope.ServiceProvider.GetRequiredService<SatelliteActionService>(),
            _ => throw new AppException("Action non trouvée", 404),
        };
    }
    
    public static IActionService Resolve(string type, IServiceScope scope)
    {
        return type switch
        {
            "muscler" => scope.ServiceProvider.GetRequiredService<MusclerActionService>(),
            "cuisiner" => scope.ServiceProvider.GetRequiredService<CuisinerActionService>(),
            "reparer" => scope.ServiceProvider.GetRequiredService<ReparerActionService>(),
            "ameliorer" => scope.ServiceProvider.GetRequiredService<AmeliorerActionService>(),
            "explorer" => scope.ServiceProvider.GetRequiredService<ExplorerActionService>(),
            "satellite" => scope.ServiceProvider.GetRequiredService<SatelliteActionService>(),
                _ => throw new AppException("Action non trouvée", 404),
        };
    }
}