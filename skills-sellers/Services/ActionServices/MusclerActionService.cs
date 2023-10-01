using skills_sellers.Entities;
using skills_sellers.Entities.Actions;

namespace skills_sellers.Services.ActionServices;

public class MusclerActionService : IActionService<ActionMuscler>
{
    public (bool valid, string why) CanExecuteAction(UserCard userCard)
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

    public Task StartAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(ActionMuscler action)
    {
        throw new NotImplementedException();
    }
}