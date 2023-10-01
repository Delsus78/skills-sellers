using skills_sellers.Entities;
using skills_sellers.Entities.Actions;

namespace skills_sellers.Services.ActionServices;

public class EtudierActionService : IActionService<ActionEtudier>
{
    public (bool valid, string why) CanExecuteAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public ActionEtudier? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionEtudier> GetActions()
    {
        throw new NotImplementedException();
    }

    public Task StartAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(ActionEtudier action)
    {
        throw new NotImplementedException();
    }
}