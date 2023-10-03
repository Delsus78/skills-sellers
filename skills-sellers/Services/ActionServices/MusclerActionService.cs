using System.Collections.Concurrent;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Models;

namespace skills_sellers.Services.ActionServices;

public class MusclerActionService : IActionService<ActionMuscler>
{
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards)
    {
        throw new NotImplementedException();
    }

    public ActionMuscler? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionMuscler> GetActions()
    {
        throw new NotImplementedException();
    }

    public Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(int actionId)
    {
        throw new NotImplementedException();
    }

    public Task RegisterNewTaskForActionAsync(ActionMuscler action, User user)
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
}