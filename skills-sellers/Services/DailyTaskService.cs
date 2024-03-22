using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Registres;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;


public interface IDailyTaskService
{
    Task ExecuteDailyTaskAsync(DataContext context);
}
public class DailyTaskService : IDailyTaskService
{
    private readonly INotificationService _notificationService;
    private readonly ISeasonService _seasonService;
    
    public DailyTaskService(
        INotificationService notificationService, ISeasonService seasonService)
    {
        _notificationService = notificationService;
        _seasonService = seasonService;
    }

    public async Task ExecuteDailyTaskAsync(DataContext context)
    {
        var today = DateTime.Today;
        var logEntry = await context.DailyTaskLog.SingleOrDefaultAsync(e => e.ExecutionDate == today.Date);
        if (logEntry == null)
        {
            Console.WriteLine("Daily task needed");
            
            // Execute the task
            try {await DailyResetBatimentDataAsync(context);} catch (Exception e) {Console.WriteLine(e);}
            try {await DailyCheckAndDeleteNotifications(context);} catch (Exception e) {Console.WriteLine(e);}
            try {await DailyCheckAndDeleteFightReports(context);} catch (Exception e) {Console.WriteLine(e);}
            try {await DailyCheckAndDeleteNeutralRegistre(context);} catch (Exception e) {Console.WriteLine(e);}
            try {await DailyExecuteFriendlyTrade(context);} catch (Exception e) {Console.WriteLine(e);}
            try {await DailyCheckEndedSeason(context);} catch (Exception e) {Console.WriteLine(e);}

            // Log the execution
            context.DailyTaskLog.Add(new DailyTaskLog { ExecutionDate = today.Date, IsExecuted = true });
            await context.SaveChangesAsync();
        }
    }

    private async Task DailyResetBatimentDataAsync(DataContext context)
    {
        var usersBatimentsData = await context.UserBatiments.ToListAsync();

        var count = 0;
        foreach (var userBatimentData in usersBatimentsData)
        {
            userBatimentData.NbCuisineUsedToday = 0;
            userBatimentData.NbBuyMarchandToday = 0;
            count++;
        }
        
        Console.WriteLine($"DailyTask : {count} users batiments data reset");
        
        // notify all users
        await _notificationService.SendNotificationToAll(new NotificationRequest(
            "Daily reset", 
            "Les cuisines ont été réinitialisées !\r\nLe marchand a été réinitialisé !", ""), context);

        await context.SaveChangesAsync();
    }

    private async Task DailyCheckAndDeleteNotifications(DataContext context)
    {
        var notifications = await context.Notifications
            .Where(notification => notification.CreatedAt.AddDays(7).Date <= DateTime.Now.Date && !notification.Title.Contains("DM") && !notification.Title.Contains("SPECIAL"))
            .ToListAsync();
        var count = 0;
        foreach (var notification in notifications)
        {
            context.Notifications.Remove(notification);
            count++;
        }
        Console.Out.WriteLine($"DailyTask : Deleted {count} notifications");
        await context.SaveChangesAsync();
    }
    
    private async Task DailyCheckAndDeleteFightReports(DataContext context)
    {
        var fightReports = await context.FightReports
            .Where(fightReport => fightReport.FightDate.AddDays(3).Date <= DateTime.Now.Date)
            .ToListAsync();
        var count = 0;
        foreach (var fightReport in fightReports)
        {
            context.FightReports.Remove(fightReport);
            count++;
        }
        Console.Out.WriteLine($"DailyTask : Deleted {count} fight reports");
        await context.SaveChangesAsync();
    }

    private async Task DailyCheckAndDeleteNeutralRegistre(DataContext context)
    {
        var registresNeutres = await context.Registres
            .OfType<RegistreNeutral>()
            .Where(registre => registre.IsFavorite == false)
            .ToListAsync();
        context.Registres.RemoveRange(registresNeutres);
        await context.SaveChangesAsync();
    }
    
    private async Task DailyExecuteFriendlyTrade(DataContext context)
    {
        var friendlyTrades = await context.Registres
            .OfType<RegistreFriendly>()
            .Include(registre => registre.User)
            .ToListAsync();

        // sort by user in dictionary
        var userTrades = new Dictionary<User, List<RegistreFriendly>>();
        foreach (var friendlyTrade in friendlyTrades)
        {
            if (!userTrades.ContainsKey(friendlyTrade.User))
                userTrades.Add(friendlyTrade.User, new List<RegistreFriendly>());
            
            userTrades[friendlyTrade.User].Add(friendlyTrade);
        }

        foreach (var tradePerUserPair in userTrades)
        {
            var user = tradePerUserPair.Key;
            var userFriendlyTrades = tradePerUserPair.Value;
            var bigNotificationMessage = "";
            
            
            foreach (var friendlyTrade in userFriendlyTrades)
            {
                var priceToPay = friendlyTrade.ResourceDemandAmount;
                var resourceType = friendlyTrade.ResourceDemand;

                var rewardAmount = friendlyTrade.ResourceOfferAmount;
                var rewardType = friendlyTrade.ResourceOffer;

                var notificationMessage = $"[{friendlyTrade.Name}]";

                #region check if user has enough resources

                if (user.GetResources()[resourceType] < priceToPay)
                {
                    // delete the registre
                    context.Registres.Remove(friendlyTrade);

                    // 50% chance to transforme the trade in a hostile trade
                    if (Randomizer.RandomInt(0, 2) == 0)
                    {
                        notificationMessage += 
                            $"HOSTILE ! Pas assez de {resourceType} ! ({user.GetResources()[resourceType]} < {priceToPay}) La planète a pris ça comme une agression (c'est des tarés) et décide de vous attaquer !";

                        // power 
                        await context.Entry(user).Collection(user => user.UserCards).LoadAsync();
                        var newHostileRegistre = WarHelpers.GenerateHostileRegistre(user, friendlyTrade.Name);

                        newHostileRegistre.Description =
                            "[Echange amical transformé en agression] " + friendlyTrade.Description;

                        context.Registres.Add(newHostileRegistre);
                    }
                    else
                    {
                        notificationMessage += $"OUCH ! Pas assez de {resourceType} ! ({user.GetResources()[resourceType]} < {priceToPay}) La planète termine les échanges avec vous.";
                    }

                    bigNotificationMessage += notificationMessage + "\r\n";
                    continue;
                }

                #endregion

                #region Success trade

                user.SetResources(rewardType, user.GetResources()[rewardType] + rewardAmount);
                user.SetResources(resourceType, user.GetResources()[resourceType] - priceToPay);

                bigNotificationMessage += notificationMessage +
                                          $"Validé ! Echange effectué ! +{rewardAmount} {rewardType} -{priceToPay} {resourceType}\r\n";
                
                #endregion
            }
            
            await _notificationService.SendNotificationToUser(user, new NotificationRequest("Echanges effectués", bigNotificationMessage, ""), context);
        }
        
        await context.SaveChangesAsync();
    }

    private async Task DailyCheckEndedSeason(DataContext context)
    {
        var season = context.Seasons
            .OrderByDescending(s=> s.Id)
            .FirstOrDefault(s => s.EndedDate == null);
        
        if (season == null)
            return;

        if (season.EndedDate <= DateTime.Now)
        {
            _seasonService.EndSeason();
        }
        
        await context.SaveChangesAsync();
    }
}