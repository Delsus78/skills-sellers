using Microsoft.EntityFrameworkCore;
 using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
 using skills_sellers.Models;
 using skills_sellers.Models.Extensions;
using skills_sellers.Services.Achievements;

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
     private readonly List<string> _legendaryAchievements = new()
     {
         "CardAtFull10"
     };
     
     public AchievementsService(DataContext context, IUserService userService, INotificationService notificationService)
     {
         _context = context;
         _userService = userService;
         _notificationService = notificationService;
     }
     
     public async Task<AchievementResponse> GetAll(int userId)
     {
         // Validation de l'entrée
         if (userId <= 0)
             throw new ArgumentException("Invalid user ID.");

         var achievement = await _context.Achievements.SingleOrDefaultAsync(a => a.UserId == userId) 
                           ?? await CreateAndSaveNewAchievement(userId);

         return achievement.ToResponse(GenerateListOfClaimableAchievements(achievement, userId));
     }

     private async Task<Achievement> CreateAndSaveNewAchievement(int userId)
     {
         var newAchievement = new Achievement { UserId = userId };
         _context.Achievements.Add(newAchievement);
         await _context.SaveChangesAsync();
         return newAchievement;
     }

     public async Task<AchievementResponse?> ClaimAchievement(User user, AchievementRequest achievementRequest)
     {
         var achievementDb = await _context.Achievements.FirstOrDefaultAsync(a => a.UserId == user.Id);
         if (achievementDb == null) return null;

         var stats = _userService.GetUserStats(user.Id);
         var userBatiment = _userService.GetUserBatiments(user.Id);
         var achievementContext = new AchievementContext();

         foreach (var strategy in AchievementContext.GetAllStrategies(stats, achievementDb, userBatiment))
         {
             achievementContext.SetStrategy(strategy);
             if (!strategy.Name.Equals(achievementRequest.AchievementName, StringComparison.OrdinalIgnoreCase) ||
                 !achievementContext.IsClaimable()) continue;
             
             // notify user
             await _notificationService.SendNotificationToUser(user, new NotificationRequest
             (
                 "Achievements !",
                 $"Votre récompense est arrivée !"
             ), _context);
             
             achievementContext.Claim(user);
             break;
         }

         await _context.SaveChangesAsync();
         return achievementDb.ToResponse(GenerateListOfClaimableAchievements(achievementDb, user.Id));
     }

     private List<string> GenerateListOfClaimableAchievements(Achievement achievement, int userId)
     {
         var stats = _userService.GetUserStats(userId);
         var userBatiment = _userService.GetUserBatiments(userId);
         var claimableAchievements = new List<string>();
         var achievementContext = new AchievementContext();
             
         foreach (var strategy in AchievementContext.GetAllStrategies(stats, achievement, userBatiment))
         {
             achievementContext.SetStrategy(strategy);
             if (achievementContext.IsClaimable()) 
                 claimableAchievements.Add(strategy.Name);
         }

         return claimableAchievements;
     }
 }