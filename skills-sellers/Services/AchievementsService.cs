using Microsoft.EntityFrameworkCore;
 using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
 using skills_sellers.Models;
 using skills_sellers.Models.Extensions;
 
 namespace skills_sellers.Services;
 
 public interface IAchievementsService
 {
     Task<AchievementResponse> GetAll(int userId);
     Task<AchievementResponse?> ClaimAchievement(User user, AchievementRequest achievement);
 }
 public class AchievementsService : IAchievementsService
 {
     private readonly DataContext _context;
     private readonly INotificationService _notificationService;
     private readonly IUserService _userService;
     
     public AchievementsService(DataContext context, IUserService userService, INotificationService notificationService)
     {
         _context = context;
         _userService = userService;
         _notificationService = notificationService;
     }
     
     public async Task<AchievementResponse> GetAll(int userId)
     {
         var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.UserId == userId);

         if (achievement != null)
             return achievement.ToResponse(GenerateListOfClaimableAchievements(achievement, userId));

         achievement = new Achievement
         {
             UserId = userId,
             CardAtStat10 = 0,
             Doublon = 0,
             Each5Cuisine = 0,
             Each5SalleDeSport = 0,
             Each5Spatioport = 0,
             CardAtFull10 = 0,
             CharismCasinoWin = 0,
             Got100RocketLaunched = 0
         };
         _context.Achievements.Add(achievement);
         await _context.SaveChangesAsync();

         return achievement.ToResponse(GenerateListOfClaimableAchievements(achievement, userId));
     }
     
     public async Task<AchievementResponse?> ClaimAchievement(User user, AchievementRequest achievement)
     {
         // check for different values
         var achievementDb = await _context.Achievements.FirstOrDefaultAsync(a => a.UserId == user.Id);
         
         if (achievementDb == null) return null;
         
         // retrouver l'achievement qui correspond au nom donné dans la requête
        
        // check if achievement is claimable
        var claimablesAchievements = GenerateListOfClaimableAchievements(achievementDb, user.Id);
        var achievements = claimablesAchievements.ToList();
        if (!achievements.Contains(achievement.AchievementName, StringComparer.OrdinalIgnoreCase))
            throw new AppException($"Achievement {achievement.AchievementName} is not claimable", 400);
        
        // update achievement
        achievementDb.ClaimAchievement(achievement);
        
        // add 1 pack to user
        user.NbCardOpeningAvailable++;
        
        // notif
        // notify user
        await _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Achievements !",
            $"Votre récompense est arrivée !"
        ), _context);

        await _context.SaveChangesAsync();
        return achievementDb.ToResponse(achievements.ToList());
     }

     private List<string> GenerateListOfClaimableAchievements(Achievement achievement, int userId)
     {
        var res = new List<string>();
        var stats = _userService.GetUserStats(userId);

        var userBatiments = _userService.GetUserBatiments(userId);
        var cuisineLevel = userBatiments.CuisineLevel;
        var salleDeSportLevel = userBatiments.SalleSportLevel;
        var spatioportLevel = userBatiments.SpatioPortLevel;

        // Casino
        if (achievement.IsClaimable(
                stats.TotalWinAtCharismeCasino.Stat, new AchievementRequest("CharismCasinoWin")))
            res.Add("CharismCasinoWin");

        // CardAtFullStat10
        if (achievement.IsClaimable(
                stats.TotalCardsFull10.Stat, new AchievementRequest("CardAtFull10")))
            res.Add("CardAtFullStat10");

        // Each5Cuisine
        if (achievement.IsClaimable(
                cuisineLevel, new AchievementRequest("Each5Cuisine"), 5, -1))
            res.Add("Each5Cuisine");
            
        // Each5SalleDeSport
        if (achievement.IsClaimable(
                salleDeSportLevel, new AchievementRequest("Each5SalleDeSport"), 5, -1))
            res.Add("Each5SalleDeSport");
        
        // Each5Spatioport
        if (achievement.IsClaimable(
                spatioportLevel, new AchievementRequest("Each5Spatioport"), 5, -1))
            res.Add("Each5Spatioport");
        
        // CardAtStat10
        if (achievement.IsClaimable(
                stats.TotalCardWithAStatMaxed.Stat, new AchievementRequest("CardAtStat10")))
            res.Add("CardAtStat10");
        
        // Doublon
        if (achievement.IsClaimable(
                stats.TotalDoublonsEarned.Stat, new AchievementRequest("Doublon")))
            res.Add("Doublon");
        
        // Got100RocketLaunched
        if (achievement.IsClaimable(
                stats.TotalRocketLaunched.Stat, new AchievementRequest("Got100RocketLaunched"), 100))
            res.Add("Got100RocketLaunched");

        return res;
     }
 }