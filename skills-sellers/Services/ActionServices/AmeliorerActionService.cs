using skills_sellers.Entities;
using skills_sellers.Entities.Actions;

namespace skills_sellers.Services.ActionServices;

public class AmeliorerActionService : IActionService<ActionAmeliorer>
{
    public (bool valid, string why) CanExecuteAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public ActionAmeliorer? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionAmeliorer> GetActions()
    {
        throw new NotImplementedException();
    }

    public Task StartAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(ActionAmeliorer action)
    {
        throw new NotImplementedException();
    }
}