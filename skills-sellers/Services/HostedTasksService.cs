using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services;

public class HostedTasksService : IHostedService
{
    // actions services
    private readonly IActionTaskService _actionTaskService;

    public HostedTasksService(
        IActionTaskService actionTaskService)
    {
        _actionTaskService = actionTaskService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // restart all actions
        await _actionTaskService.RestartAllActionsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // stop all actions
        return _actionTaskService.StopAllActionsAsync();
    }
}