using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Formatting = System.Xml.Formatting;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace skills_sellers.Services;

public interface ISeasonService
{
    SeasonResponse GetActualSeason();
    SeasonResponse EndSeason();
    SeasonResponse StartSeason(int day);
}
public class SeasonService : ISeasonService
{
    private readonly DataContext _context;
    private readonly INotificationService _notificationService;

    public SeasonService(DataContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public SeasonResponse GetActualSeason()
    {
        var season = _context.Seasons.OrderByDescending(s=> s.Id).FirstOrDefault();
        if (season == null)
            season = new Season
            {
                StartedDate = DateTime.Now, 
                Duration = new TimeSpan(0, 0, 0, 0), 
                EndedDate = DateTime.Now
            };

        var winner = season.WinnerId != null ? _context.Users.SingleOrDefault(u => u.Id == season.WinnerId) : null;
        
        return season.ToResponse(winner);
    }

    public SeasonResponse EndSeason()
    {
        var season = _context.Seasons.OrderByDescending(s=> s.Id).FirstOrDefault();
        if (season == null)
        {
            throw new Exception("No season is currently running");
        }

        season.EndedDate = DateTime.Now;
        PrintAndSaveAllPlayersStatsIfSeasonIsOver(season);
        
        var winner = _context.Users.OrderByDescending(u => u.Score).First();
        
        season.WinnerId = winner.Id;

        _notificationService.SendNotificationToAll(
            new NotificationRequest("[SPECIAL] FIN DE LA SAISON " + season.Id, 
                "La saison est terminée, le gagnant est " + winner.Pseudo + " avec un score de "+ winner.Score +" !",
                ""), _context);

        _context.SaveChanges();
        
        return season.ToResponse(winner);
    }

    public SeasonResponse StartSeason(int day)
    {
        var season = _context.Seasons.SingleOrDefault(s => s.EndedDate == null);
        if (season != null)
        {
            throw new Exception("A season is already running");
        }

        season = new Season
        {
            StartedDate = DateTime.Now,
            Duration = new TimeSpan(day, 0, 0, 0)
        };
        
        _context.Seasons.Add(season);
        _context.SaveChanges();
        
        return season.ToResponse();
    }

    private void PrintAndSaveAllPlayersStatsIfSeasonIsOver(Season season)
    {
        // GET ALL DATA 
        var allPlayersData = _context.Users
            .Include(u => u.Stats)
            .Include(u => u.Registres)
            .Include(u => u.Achievement)
            .Include(u => u.UserBatimentData)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Competences)
            .Include(u => u.UserWeapons)
            .Include(u => u.UserCardsDoubled)
            .Include(u => u.UserCosmetics)
            .Include(u => u.UserRegistreInfo)
            .ToList();
        
        // SAVE ALL DATA
        var json = JsonConvert.SerializeObject(allPlayersData, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings 
        { 
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
        
        Console.Out.WriteLine(json);
    }
}
