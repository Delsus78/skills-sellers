using skills_sellers.Entities;
using skills_sellers.Entities.Actions;

namespace skills_sellers.Services.ActionServices;

public class ExplorerActionService : IActionService<ActionExplorer>
{
    public (bool valid, string why) CanExecuteAction(UserCard userCard)
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

    public Task StartAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(ActionExplorer action)
    {
        throw new NotImplementedException();
    }
}