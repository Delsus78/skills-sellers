using Microsoft.EntityFrameworkCore;
 using skills_sellers.Entities;
 using skills_sellers.Helpers.Bdd;
 using skills_sellers.Models;
 using skills_sellers.Models.Extensions;
 
 namespace skills_sellers.Services;
 
 public interface IAchievementsService
 {
     Task<AchievementResponse> GetAll(User user);
     Task<AchievementResponse?> Update(User user, AchievementResponse achievement);
 }
 public class AchievementsService : IAchievementsService
 {
     private readonly DataContext _context;
     
     public AchievementsService(DataContext context)
     {
         _context = context;
     }
     
     public async Task<AchievementResponse> GetAll(User user)
     {
         var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.UserId == user.Id);
 
         if (achievement != null) return achievement.ToResponse(new List<string>());
         
         achievement = new Achievement
         {
             UserId = user.Id,
             CardAtStat10 = 0,
             Doublon = 0,
             Each5Cuisine = 0,
             Each5SalleDeSport = 0,
             Each5Spatioport = 0,
             CardAtFull10 = 0,
             CharismCasinoWin = 0
         };
         _context.Achievements.Add(achievement);
         await _context.SaveChangesAsync();
 
         return achievement.ToResponse(new List<string>());
     }
 
     public async Task<AchievementResponse?> Update(User user, AchievementResponse achievement)
     {
         // check for different values
         var achievementDb = await _context.Achievements.FirstOrDefaultAsync(a => a.UserId == user.Id);
         
         if (achievementDb == null) return null;
         
         // lister toutes les propriétés de achievement qui sont différentes de achievementDb
         var properties = achievement.GetType().GetProperties();
         var propertiesToUpdate = 
             (from property in properties let value = 
                 property.GetValue(achievement) 
                 let valueDb = 
                     property.GetValue(achievementDb) 
                 where value != null && valueDb != null && !value.Equals(valueDb) 
                 select property.Name).ToList();
         
         // si aucune propriété n'est différente, on ne fait rien
         if (propertiesToUpdate.Count == 0) return achievementDb.ToResponse(new List<string>());
         
         // sinon, cela veut dire que TODO
         return null;
     }

     private AchievementResponse CheckForClaimableAchievement(Achievement achievement)
     {
         // TODO
         throw new NotImplementedException();
     }
 }