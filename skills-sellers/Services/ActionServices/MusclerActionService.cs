using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services.ActionServices;

public class MusclerActionService : IActionService<ActionMuscler>
{
    private DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IServiceProvider _serviceProvider;
    
    public MusclerActionService(
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

    public ActionMuscler? GetAction(UserCard userCard)
    {
        throw new NotImplementedException();
    }

    public List<ActionMuscler> GetActions()
    {
        return IncludeGetActionsMuscler().ToList();
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

    public Task RegisterNewTaskForActionAsync(ActionMuscler action, User user)
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations { get; } = new();
    
    // Helpers
    
    private IIncludableQueryable<ActionMuscler,Object> IncludeGetActionsMuscler()
    {
        return _context.Actions
            .OfType<ActionMuscler>()
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
}