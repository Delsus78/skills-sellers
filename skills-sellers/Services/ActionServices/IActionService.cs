using skills_sellers.Entities;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public interface IActionService<T> where T : Action
{
    (bool valid, string why) CanExecuteAction(UserCard userCard);
    
    T? GetAction(UserCard userCard);
    
    List<T> GetActions();
    
    Task StartAction(UserCard userCard);
    
    Task EndAction(T action);
}