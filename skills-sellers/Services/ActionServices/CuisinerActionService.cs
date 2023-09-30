using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services.ActionServices;

public class CuisinerActionService : IActionService<ActionCuisiner>
{
    private readonly DataContext _context;
    
    public CuisinerActionService(DataContext context)
    {
        _context = context;
    }

    #region Validator

    public (bool valid, string why) CanExecuteAction(UserCard userCard)
    {
        // Carte déjà en action
        if (GetAction(userCard) != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        // TODO
        
        // Stats et ressources suffisantes ?
        // Cuisiner ne nécessite pas de ressources ni de minimum de stats
        
        return (true, "");
    }

    #endregion

    #region Starters
    
    public async Task StartAction(UserCard userCard)
    {
        var validation = CanExecuteAction(userCard);
        if (!validation.valid)
            throw new AppException("Impossible de cuisiner : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();
        
        // Random plat
        var randomPlat = FoodRandomizer.RandomPlat();
        
        var action = new ActionCuisiner
        {
            UserCards = new List<UserCard> { userCard },
            DueDate = endTime,
            Plat = randomPlat
        };

        // TODO trigger on action start du batiment utilisé

        await _context.ActionsCuisiner.AddAsync(action);
        await _context.SaveChangesAsync();
        
        // start timer
        
    }

    public Task EndAction(ActionCuisiner action)
    {
        throw new NotImplementedException();
    }

    #endregion
    
    public ActionCuisiner? GetAction(UserCard userCard)
    {
        return IncludeGetActionsCuisiner()
            .FirstOrDefault(a => a.UserCards
                .Any(uc => uc.CardId == userCard.CardId 
                               && uc.UserId == userCard.UserId));
    }
    
    public List<ActionCuisiner> GetActions()
    {
        return IncludeGetActionsCuisiner().ToList();
    }
    
    // Helpers
    
    private IIncludableQueryable<ActionCuisiner,Object> IncludeGetActionsCuisiner()
    {
        return _context.ActionsCuisiner
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Card);
    } 
    
    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddMinutes(30);
    }
}