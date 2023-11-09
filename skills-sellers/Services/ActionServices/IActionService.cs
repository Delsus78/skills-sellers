using skills_sellers.Entities;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public interface IActionService
{
    Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider);
    Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider);
    Task<Action> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider);

    (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model);
    ActionEstimationResponse EstimateAction(User user, ActionRequest model);
}