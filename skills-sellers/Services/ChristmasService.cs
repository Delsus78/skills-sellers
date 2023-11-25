using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Speciales;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services;

public interface IChristmasService
{
    Task<Christmas> GetDaysGiftOpened(User user);
    Task<Christmas> OpenDayGift(User user);
}
public class ChristmasService : IChristmasService
{
    private readonly DataContext _context;
    private readonly INotificationService _notificationService;

    public ChristmasService(DataContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private DateTime _now = DateTime.Now;
    
    /// <summary>
    /// 0 is not claimable
    /// 1 is claimable
    /// 2 is claimed
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Christmas> GetDaysGiftOpened(User user)
    {
        var christmas = await _context.Christmas.FirstOrDefaultAsync(c => c.UserId == user.Id);
        
        if (christmas == null)
        {
            christmas = new Christmas
            {
                UserId = user.Id,
                // init list with 24 days
                DaysOpened = new List<int>
                {
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0
                }
            };
            
            await _context.Christmas.AddAsync(christmas);
        }

        // set today as claimable if not claimed yet
        if (IsChristmas()) RefreshDayClaimability(christmas);

        await _context.SaveChangesAsync();

        return christmas;
    }
    
    public async Task<Christmas> OpenDayGift(User user)
    {
        var christmas = await GetDaysGiftOpened(user);
        
        
        // check if we are between 1 and 24 december
        if (!IsChristmas())
            throw new AppException("Vous ne pouvez pas ouvrir de cadeau en dehors de la période de Noël.", 400);

        var day = _now.Day;
        if (christmas.DaysOpened[day - 1] == 2)
            throw new AppException("Vous avez déjà ouvert ce cadeau.", 400);
        
        christmas.DaysOpened[day - 1] = 2;
        
        // give gift
        await GiveGift(user.Id, day);

        await _context.SaveChangesAsync();

        return christmas;
    }
    
    private bool IsChristmas()
    {
        return _now is { Month: 12, Day: < 25 };
    }
    
    private async Task GiveGift(int userId, int day)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return;

        var notificationMessage = "Joyeux Noël ! Vous recevez : ";
        
        // give gift
        switch (day)
        {
            case 1:
                user.NbCardOpeningAvailable += 1;
                notificationMessage += "1 ouverture de carte.";
                break;
            
            case 2:
                user.Creatium += 500;
                notificationMessage += "500 créatium.";
                break;
            
            case 3:
                user.Or += 250;
                notificationMessage += "250 or.";
                break;
            
            case 4:
                user.Nourriture += 2;
                notificationMessage += "2 nourriture.";
                break;
            
            case 5:
                user.NbCardOpeningAvailable += 2;
                notificationMessage += "2 ouvertures de carte.";
                break;
            
            case 6:
                user.Creatium += 600;
                notificationMessage += "600 créatium.";
                break;
            
            case 7:
                user.Or += 500;
                notificationMessage += "500 or.";
                break;
            
            case 8:
                user.Nourriture += 4;
                notificationMessage += "4 nourriture.";
                break;
            
            case 9:
                user.NbCardOpeningAvailable += 1;
                notificationMessage += "1 ouverture de carte.";
                break;
            
            case 10:
                user.NbCardOpeningAvailable += 3;
                notificationMessage += "3 ouvertures de carte.";
                break;
            
            case 11:
                user.Nourriture += 4;
                notificationMessage += "4 nourritures.";
                break;
            
            case 12:
                user.Creatium += 1000;
                notificationMessage += "1000 créatium.";
                break;
            
            case 13:
                user.Or += 1000;
                notificationMessage += "1000 or.";
                break;
            
            case 14:
                user.Creatium += 2000;
                notificationMessage += "2000 créatium.";
                break;
            
            case 15:
                user.NbCardOpeningAvailable += 4;
                notificationMessage += "4 ouvertures de carte.";
                break;
            
            case 16:
                user.Or += 1500;
                notificationMessage += "1500 or.";
                break;
            
            case 17:
                user.Creatium += 3000;
                notificationMessage += "3000 créatium.";
                break;
            
            case 18:
                user.Nourriture += 10;
                notificationMessage += "10 nourriture.";
                break;
            
            case 19:
                user.Or += 2000;
                notificationMessage += "2000 or.";
                break;
            
            case 20:
                user.NbCardOpeningAvailable += 5;
                notificationMessage += "5 ouvertures de carte.";
                break;
            
            case 21:
                user.Creatium += 5000;
                notificationMessage += "5000 créatium.";
                break;
            
            case 22:
                user.Nourriture += 20;
                notificationMessage += "20 nourriture.";
                break;
            
            case 23:
                user.Or += 4000;
                notificationMessage += "4000 or.";
                break;
            
            case 24:
                user.NbCardOpeningAvailable += 24;
                notificationMessage += "24 ouvertures de carte.";
                break;
        }
        
        // notify user
        await _notificationService.SendNotificationToUser(user, new NotificationRequest(
                "SPECIAL Cadeau de Noël !", notificationMessage), _context);
    }
    
    private void RefreshDayClaimability(Christmas christmas)
    {
        // for each day
        for (var i = 0; i < christmas.DaysOpened.Count; i++)
        {
            // set 0 if day is in the past and not claimed yet
            if (i < _now.Day - 1 && christmas.DaysOpened[i] == 1)
                christmas.DaysOpened[i] = 0;
            
            // set 1 if day is today and not claimed yet
            if (i == _now.Day - 1 && christmas.DaysOpened[i] == 0)
                christmas.DaysOpened[i] = 1;
            
            // set 0 if the day is in the future and not claimed yet
            if (i > _now.Day - 1 && christmas.DaysOpened[i] == 1)
                christmas.DaysOpened[i] = 0;
        }
    }
}