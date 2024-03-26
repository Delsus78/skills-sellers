using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class ReparerActionService : IActionService
{
    private readonly INotificationService _notificationService;
    private readonly IWeaponService _weaponService;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IStatsService _statsService;

    public ReparerActionService(
        INotificationService notificationService, 
        IWeaponService weaponService,
        IStatsService statsService,
        IUserBatimentsService userBatimentsService)
    {
        _notificationService = notificationService;
        _weaponService = weaponService;
        _statsService = statsService;
        _userBatimentsService = userBatimentsService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        // is Zeiss Machine day ?
        if (DateTime.Now.DayOfWeek != DayOfWeek.Tuesday && DateTime.Now.DayOfWeek != DayOfWeek.Friday)
            return (false, "Ce n'est pas le jour de la machine de Zeiss !");
        
        if (_userBatimentsService.IsUserBatimentFull(user, "machineZeiss"))
            return (false, "Vous avez déjà une machine de Zeiss en action !");
        
        if (userCards.Count < 1)
            return (false, "Vous devez déposer au moins une carte !");
        
        // cards already in action ?
        if (userCards.Any(c => c.Action != null))
            return (false, "Une de vos cartes est déjà en action !");
        
        // check if user has enough resources
        var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(userCards.Count, user.UserWeapons.Count);
        
        if (user.Creatium < creatiumPrice)
            return (false, $"Vous n'avez pas assez de créatium ! Il vous en manque {creatiumPrice - user.Creatium}");

        if (user.Or < orPrice)
            return (false, $"Vous n'avez pas assez d'or ! Il vous en manque {orPrice - user.Or}");

        return (true, "");
    }

    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible ! " + validation.why, 400);
        
        // set cards in action
        var totalIntel = userCards.Sum(c => c.Competences.Intelligence);
        var chances = CalculateRepairChances(totalIntel, user.UserCards.Count);
        
        var action = new ActionReparer
        {
            UserCards = userCards,
            DueDate = CalculateActionEndTime(),
            RepairChances = chances,
            User = user
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // consume resources
        var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(userCards.Count, user.UserWeapons.Count);
        user.Creatium -= creatiumPrice;
        user.Or -= orPrice;
        
        await context.SaveChangesAsync();

        // return response
        return new List<Action> { action };
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
        var totalUserCards = user.UserCards.Count;

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        
        // calculate action end time and resources needed
        var endTime = CalculateActionEndTime();
        var totalIntel = userCards.Sum(c => c.Competences.Intelligence);
        var chances = CalculateRepairChances(totalIntel, user.UserCards.Count);
        var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(totalUserCards, user.UserWeapons.Count);
        
        return new ActionEstimationResponse
        {
            EndDates = new List<DateTime> { endTime },
            ActionName = "reparer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "chances", chances.ToString("F2") + "%" }
            },
            Couts = new Dictionary<string, string>
            {
                { "de créatium", creatiumPrice.ToString() },
                { "d'or", orPrice.ToString() }
            },
            Error = !validation.valid ? "Impossible d'utiliser la machine : " + validation.why : null
        };
    }

    public async Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        if (action is not ActionReparer actionReparer)
            throw new AppException("Action is not a reparer action", 400);
        
        // calculate chances (random double between 0 and 100)
        var chances = Randomizer.RandomDouble(0, 100);
        
        if (chances < actionReparer.RepairChances || actionReparer.RepairChances >= 100)
        {
            
            // logs
            Console.Out.WriteLine($"[ZEISS] {user.Pseudo} a construit une arme avec {actionReparer.RepairChances} de chances");
            
            // notify user
            await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Construction d'arme terminée", 
                    $"La machine de Zeiss a terminée la construction d'une nouvelle arme! Vous pouvez maintenant l'utiliser !", 
                    "weapons"), 
                context);
            
            // add weapon to user
            user.NbWeaponOpeningAvailable++;

            // stats
            _statsService.OnMachineUsed(user.Id);
        }
        else 
        {
            await _notificationService.SendNotificationToUser(action.User, new NotificationRequest(
                    "Construction d'arme échouée",
                    $"Malheureusement, la machine n'a pas réussi à construire une arme... Vous pouvez retenter votre chance !", ""),
                context);
            
            // refund user half
            var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(action.UserCards.Count, user.UserWeapons.Count);
            user.Creatium += creatiumPrice / 2;
            user.Or += orPrice / 2;
        }
        
        // remove action
        context.Actions.Remove(action);
        
        await context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        context.Actions.Remove(action);
        
        // refund
        var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(action.UserCards.Count, user.UserWeapons.Count);
        user.Creatium += creatiumPrice;
        user.Or += orPrice;

        return context.SaveChangesAsync();
    }

    // Helpers
    private DateTime CalculateActionEndTime()
    {
         return DateTime.Now.AddHours(5);
         //return DateTime.Now.AddSeconds(10);
    }
    
    private double CalculateRepairChances(int totalIntel, int totalCards)
    {
        return totalIntel / ((double)totalCards * 4) * 100;
    }
}
