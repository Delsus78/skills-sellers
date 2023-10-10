using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services;

public class HostedTasksService : IHostedService
{
    // actions services
    private readonly IServiceProvider _serviceProvider;

    public HostedTasksService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        // Récupérer le contexte
        await using var context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
            
        // Récupérer toutes les actions en cours et les lancer
        var ongoingActions = context.Actions.Include(a => a.User).ToList();
        var taskList = new List<Task>();
        // lancer chaque type d'action via son service
            
        foreach (var action in ongoingActions)
            switch (action)
            {
                case ActionCuisiner actionCuisiner:
                    taskList.Add(
                        scope.ServiceProvider.GetRequiredService<IActionService<ActionCuisiner>>()
                            .RegisterNewTaskForActionAsync(actionCuisiner, actionCuisiner.User));
                    break;
                case ActionExplorer actionExplorer:
                    taskList.Add(
                        scope.ServiceProvider.GetRequiredService<IActionService<ActionExplorer>>()
                            .RegisterNewTaskForActionAsync(actionExplorer, actionExplorer.User));
                    break;
                case ActionMuscler actionMuscler:
                    taskList.Add(
                        scope.ServiceProvider.GetRequiredService<IActionService<ActionMuscler>>()
                            .RegisterNewTaskForActionAsync(actionMuscler, actionMuscler.User));
                    break;
                case ActionAmeliorer actionAmeliorer:
                    taskList.Add(
                        scope.ServiceProvider.GetRequiredService<IActionService<ActionAmeliorer>>()
                            .RegisterNewTaskForActionAsync(actionAmeliorer, actionAmeliorer.User));
                    break;
            }

        _ = Task.WhenAll(taskList).ContinueWith(t =>
        {
            if (t is { IsFaulted: true, Exception: not null })
            {
                Console.Error.WriteLine(t.Exception);
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            // Annuler toutes les actions en cours
            scope.ServiceProvider.GetRequiredService<IActionService<ActionCuisiner>>()
                .TaskCancellations.Values.ToList()
                .ForEach(cts => cts.Cancel());
            
            scope.ServiceProvider.GetRequiredService<IActionService<ActionExplorer>>()
                .TaskCancellations.Values.ToList()
                .ForEach(cts => cts.Cancel());
            
            scope.ServiceProvider.GetRequiredService<IActionService<ActionMuscler>>()
                .TaskCancellations.Values.ToList()
                .ForEach(cts => cts.Cancel());
            
            scope.ServiceProvider.GetRequiredService<IActionService<ActionAmeliorer>>()
                .TaskCancellations.Values.ToList()
                .ForEach(cts => cts.Cancel());
        }
        return Task.CompletedTask;
    }
}