using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services.ActionServices;

public class AmeliorerActionService : IActionService<ActionAmeliorer>
{
    private DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    
    public AmeliorerActionService(
        DataContext context,
        IUserBatimentsService userBatimentsService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
        _serviceProvider = serviceProvider;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards)
    {
        throw new NotImplementedException();
    }

    public ActionAmeliorer? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionAmeliorer> GetActions()
    {
        return IncludeGetActionsAmeliorer().ToList();
    }

    public Task<ActionResponse> StartAction(User user, ActionRequest model)
    {
        throw new NotImplementedException();
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        throw new NotImplementedException();
    }

    public Task EndAction(int actionId)
    {
        throw new NotImplementedException();
    }

    public Task RegisterNewTaskForActionAsync(ActionAmeliorer action, User user)
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    // Helpers
    
    private IIncludableQueryable<ActionAmeliorer,Object> IncludeGetActionsAmeliorer()
    {
        return _context.Actions
            .OfType<ActionAmeliorer>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
}