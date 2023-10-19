using System.Collections.Concurrent;
using skills_sellers.Entities;
using skills_sellers.Models;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public interface IActionService<T> where T : Action
{
    (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model);
    
    T? GetAction(UserCard userCard);
    
    List<T> GetActions();

    Task<ActionResponse> StartAction(User user, ActionRequest model);
    
    ActionEstimationResponse EstimateAction(User user, ActionRequest model);

    Task EndAction(int actionId);

    Task DeleteAction(User user, int actionId);
    
    Task RegisterNewTaskForActionAsync(T action, User user);

    ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; }
}
