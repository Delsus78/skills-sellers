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
    Task<ActionResponse> CreateNewActionAsync(User user, ActionRequest model);
    Task StartNewTaskForAction(Action action);
    Task DeleteActionAsync(int actionId);
}
public class ActionTaskService : IActionTaskService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActionServiceResolver _actionServiceResolver;
    private ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    public ActionTaskService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _actionServiceResolver = new ActionServiceResolver(serviceProvider);
    }

    public async Task<ActionResponse> CreateNewActionAsync(User user, ActionRequest model)
    {
        var service = _actionServiceResolver.Resolve(model.ActionName.ToLower());
        var context = _serviceProvider.GetRequiredService<DataContext>();
        
        // start action
        var responseAction = await service.StartAction(user, model, context, _serviceProvider);
        
        // start timer
        _ = StartNewTaskForAction(responseAction);

        return responseAction.ToResponse();
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
    
    public async Task DeleteActionAsync(int actionId)
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
        }
        else
        {
            // La date d'échéance est déjà passée
            await DispatchToCorrectEndActionService(action.Id);
        }
        
        TaskCancellations.TryRemove(action.Id, out _);
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
        {
            // Vous pourriez envisager de retourner false ou un résultat personnalisé ici pour indiquer une erreur
            throw new AppException("Action non trouvée", 404);
        }

        var service = _actionServiceResolver.Resolve(actionEntity);
        await actionFunc(service, actionEntity, context, _serviceProvider);
    }

    private Task DispatchToCorrectEndActionService(int actionId) =>
        DispatchToCorrectService(actionId,
            (service, action, context, serviceProvider) 
                => service.EndAction(action, context, serviceProvider));
    
    private Task DispatchToCorrectDeleteActionService(int actionId) =>
        DispatchToCorrectService(actionId, 
            (service, action, context, serviceProvider) 
            => service.DeleteAction(action, context, serviceProvider));

    #endregion
}

// Factory ou stratégie pour résoudre le service d'action approprié
public class ActionServiceResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ActionServiceResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IActionService Resolve(Action action)
    {
        return action switch
        {
            ActionMuscler _ => _serviceProvider.GetRequiredService<MusclerActionService>(),
            ActionCuisiner _ => _serviceProvider.GetRequiredService<CuisinerActionService>(),
            ActionReparer _ => _serviceProvider.GetRequiredService<ReparerActionService>(),
            ActionAmeliorer _ => _serviceProvider.GetRequiredService<AmeliorerActionService>(),
            ActionExplorer _ => _serviceProvider.GetRequiredService<ExplorerActionService>(),
            _ => throw new AppException("Action non trouvée", 404),
        };
    }
    
    public IActionService Resolve(string type)
    {
        return type switch
        {
            "muscler" => _serviceProvider.GetRequiredService<MusclerActionService>(),
            "cuisiner" => _serviceProvider.GetRequiredService<CuisinerActionService>(),
            "reparer" => _serviceProvider.GetRequiredService<ReparerActionService>(),
            "ameliorer" => _serviceProvider.GetRequiredService<AmeliorerActionService>(),
            "explorer" => _serviceProvider.GetRequiredService<ExplorerActionService>(),
            _ => throw new AppException("Action non trouvée", 404),
        };
    }
}