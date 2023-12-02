using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;


public interface IUserBatimentsService
{
    UserBatimentData GetOrCreateUserBatimentData(User user, DataContext? context = null);
    
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
    bool IsUserBatimentFull(User user, string batimentName, DataContext? context = null);
    
    (int creatiumPrice, int intelPrice, int forcePrice) GetBatimentPrices(int batimentLevel, string batimentName);
}
public class UserBatimentsService : IUserBatimentsService
{
    private DataContext _context;
    
    public UserBatimentsService(DataContext context)
    {
        _context = context;
    }
    
    public UserBatimentData GetOrCreateUserBatimentData(User user, DataContext? context = null)
    {
        _context = context ?? _context;
        
        if (user == null)
            throw new AppException("User not found", 404);
        
        var userBatimentData = IncludeGetUserBatimentDatas().ToList().FirstOrDefault(
        ub => ub.User.Id == user.Id);
        
        if (userBatimentData != null)
            return userBatimentData;

        // create new userBatimentData
        userBatimentData = new UserBatimentData
        {
            CuisineLevel = 1,
            SalleSportLevel = 1,
            LaboLevel = 1,
            SpatioPortLevel = 1
        };
        user.UserBatimentData = userBatimentData;
        return userBatimentData;
    }

    public bool IsUserBatimentFull(User user, string batimentName, DataContext? context = null)
    {
        _context = context ?? _context;
        
        var userBatimentData = GetOrCreateUserBatimentData(user);
        var batNameLower = batimentName.ToLower();
        var actionCounts = user.UserCards.FindAll(uc => uc.Action != null)
            .GroupBy(uc => uc.Action!.GetType())
            .ToDictionary(g => g.Key, g => g.Count());
        
        (int nbActionsEnCours, int batLevel) = batNameLower switch
        {
            "cuisine" => (userBatimentData.NbCuisineUsedToday, userBatimentData.CuisineLevel),
            "salledesport" => (actionCounts.GetValueOrDefault(typeof(ActionMuscler), 0), userBatimentData.SalleSportLevel),
            "laboratoire" => (actionCounts.GetValueOrDefault(typeof(ActionAmeliorer), 0), userBatimentData.LaboLevel),
            "spatioport" => (actionCounts.GetValueOrDefault(typeof(ActionExplorer), 0), userBatimentData.SpatioPortLevel),
            "satellite" => (actionCounts.GetValueOrDefault(typeof(ActionSatellite), 0), userBatimentData.SatelliteLevel),
            "machinezeiss" => (actionCounts.GetValueOrDefault(typeof(ActionReparer), 0), 1),
            _ => throw new AppException("Batiment name not found", 404)
        };

        return nbActionsEnCours >= batLevel;
    }

    public (int creatiumPrice, int intelPrice, int forcePrice) GetBatimentPrices(int batimentLevel, string batimentName)
    {
        return (GetCreatiumBatimentPrice(batimentLevel, batimentName), GetIntelBatimentPrice(batimentLevel, batimentName), GetForceBatimentPrice(batimentLevel, batimentName));
    }

    private int GetCreatiumBatimentPrice(int currentLevel, string batimentName)
    {
        var price = (int)(Math.Pow(1.3, currentLevel) * 400);
        
        // speciales cases
        if (batimentName.ToLower() == "satellite")
            if (currentLevel == 0)
                price *= 10;
            else price *= 50;
        
        return price;
    }

    private int GetIntelBatimentPrice(int currentLevel, string batimentName)
    {
        var price = currentLevel * 2;
        
        // speciales cases
        if (batimentName.ToLower() == "satellite")
            price *= 2;
        
        return price;
    }

    private int GetForceBatimentPrice(int currentLevel, string batimentName)
    {
        var price = currentLevel * 4;
        
        // speciales cases
        if (batimentName.ToLower() == "satellite")
            price *= 2;
        
        return price;
    }

    private IIncludableQueryable<UserBatimentData, Object> IncludeGetUserBatimentDatas()
    {
        return _context.UserBatiments.Include(ub => ub.User);
    }
}