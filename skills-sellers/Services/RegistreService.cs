using skills_sellers.Entities;
using skills_sellers.Entities.Registres;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface IRegistreService
{
    Task DeleteFriendlyRegistre(User user, int registreId);
    IEnumerable<FightReportResponse> GetFightReports(int limit);
    Task SwitchFavorite(User user, int registreId);
}
public class RegistreService : IRegistreService
{
    private readonly DataContext _context;
    private readonly INotificationService _notificationService;

    public RegistreService(DataContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public Task DeleteFriendlyRegistre(User user, int registreId)
    {
        var registre = _context.Registres.FirstOrDefault(r => r.Id == registreId);
        
        if (registre == null)
            throw new AppException("Registre not found", 404);
        
        if (registre.Type != RegistreType.Friendly)
            throw new AppException("Registre is not friendly", 400);
        
        if (registre.UserId != user.Id)
            throw new AppException("Registre is not yours", 400);
        
        // pass the registre to Neutral
        var neutralRegistre = new RegistreNeutral
        {
            Name = registre.Name,
            Description = registre.Description,
            EncounterDate = registre.EncounterDate,
            UserId = registre.UserId
        };
        
        _context.Registres.Remove(registre);
        
        _context.Registres.Add(neutralRegistre);

        // notify user
        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Registre supprimé",
            "Vous avez rompu tout lien avec " + registre.Name + ".\n" +
            "Ils ont bien compris le message...",
            ""
        ), _context);

        return _context.SaveChangesAsync();
    }

    public IEnumerable<FightReportResponse> GetFightReports(int limit) =>
        _context.FightReports
            .OrderByDescending(fr => fr.FightDate)
            .Take(limit)
            .Select(fr => fr.ToResponse())
            .ToList();

    public Task SwitchFavorite(User user, int registreId)
    {
        var registre = _context.Registres.FirstOrDefault(r => r.Id == registreId);
        
        if (registre == null)
            throw new AppException("Registre not found", 404);
        
        if (registre is not RegistreNeutral registreNeutral)
            throw new AppException("Registre is not neutral", 400);
        
        if (registre.UserId != user.Id)
            throw new AppException("Registre is not yours", 400);
        
        registreNeutral.IsFavorite ??= false;
        registreNeutral.IsFavorite = !registreNeutral.IsFavorite;
        
        return _context.SaveChangesAsync();
    }
}