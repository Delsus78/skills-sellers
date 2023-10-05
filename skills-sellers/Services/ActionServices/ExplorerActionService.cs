using System.Collections.Concurrent;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Models;

namespace skills_sellers.Services.ActionServices;

public class ExplorerActionService : IActionService<ActionExplorer>
{
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards)
    {
        throw new NotImplementedException();
    }

    public ActionExplorer? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionExplorer> GetActions()
    {
        throw new NotImplementedException();
    }

    public Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        throw new NotImplementedException();
    }

    public ActionResponse EstimateAction(User user, ActionRequest model)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(int actionId)
    {
        throw new NotImplementedException();
    }

    public Task RegisterNewTaskForActionAsync(ActionExplorer action, User user)
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
}