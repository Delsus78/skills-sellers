using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;


public interface IUserBatimentsService
{
    UserBatimentData GetOrCreateUserBatimentData(User user);
    
    /// <summary>
    /// Retourne true si une carte de l'utilisateur est déjà en train d'effectuer une action du type correspondant au batiment
    /// </summary>
    /// <param name="user">the user given mus come with UserCards included and their own actions too</param>
    /// <param name="batimentName">
    /// - cuisine
    /// - salleDeSport
    /// - laboratoire
    /// - spatioport
    /// </param>
    /// <returns></returns>
    bool IsUserBatimentFull(User user, string batimentName);
    
    (int creatiumPrice, int intelPrice, int forcePrice) GetBatimentPrices(int batimentLevel);
}
public class UserBatimentsService : IUserBatimentsService
{
    private readonly DataContext _context;
    
    public UserBatimentsService(DataContext context)
    {
        _context = context;
    }
    
    public UserBatimentData GetOrCreateUserBatimentData(User user)
    {
        if (user == null)
            throw new AppException("User not found", 404);
        
        var userBatimentData = IncludeGetUserBatimentDatas().ToList().FirstOrDefault(
        ub => ub.User.Id == user.Id,
        new UserBatimentData
        {
            User = user,
            CuisineLevel = 1,
            SalleSportLevel = 1,
            LaboLevel = 1,
            SpatioPortLevel = 1
        });
        
        user.UserBatimentData = userBatimentData;
        _context.SaveChanges();
        return userBatimentData;
    }

    public bool IsUserBatimentFull(User user, string batimentName)
    {
        var userBatimentData = GetOrCreateUserBatimentData(user);
        var batNameLower = batimentName.ToLower();
        var actionCounts = user.UserCards.FindAll(uc => uc.Action != null)
            .GroupBy(uc => uc.Action!.GetType())
            .ToDictionary(g => g.Key, g => g.Count());
        
        (int nbActionsEnCours, int batLevel) = batNameLower switch
        {
            "cuisine" => (actionCounts.GetValueOrDefault(typeof(ActionCuisiner), 0), userBatimentData.CuisineLevel),
            "salleDeSport" => (actionCounts.GetValueOrDefault(typeof(ActionMuscler), 0), userBatimentData.SalleSportLevel),
            "laboratoire" => (actionCounts.GetValueOrDefault(typeof(ActionAmeliorer), 0), userBatimentData.LaboLevel),
            "spatioport" => (actionCounts.GetValueOrDefault(typeof(ActionExplorer), 0), userBatimentData.SpatioPortLevel),
            _ => throw new AppException("Batiment name not found", 404)
        };

        return nbActionsEnCours >= batLevel;
    }

    public (int creatiumPrice, int intelPrice, int forcePrice) GetBatimentPrices(int batimentLevel)
    {
        return (GetCreatiumBatimentPrice(batimentLevel), GetIntelBatimentPrice(batimentLevel), GetForceBatimentPrice(batimentLevel));
    }

    private int GetCreatiumBatimentPrice(int currentLevel)
    {
        return (int)(Math.Pow(1.3, currentLevel) * 400);
    }

    private int GetIntelBatimentPrice(int currentLevel)
    {
        return (currentLevel + 1) * 2;
    }

    private int GetForceBatimentPrice(int currentLevel)
    {
        return (currentLevel + 1) * 4;
    }
    
    public IIncludableQueryable<UserBatimentData, Object> IncludeGetUserBatimentDatas()
    {
        return _context.UserBatiments.Include(ub => ub.User);
    }
}