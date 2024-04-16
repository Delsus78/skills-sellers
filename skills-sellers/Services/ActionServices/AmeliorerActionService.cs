using System.Globalization;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class AmeliorerActionService : IActionService
{
    private readonly INotificationService _notificationService;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly IWeaponService _weaponService;
    private readonly IStatsService _statsService;
    private readonly DataContext _context;

    public AmeliorerActionService(
        IUserBatimentsService userBatimentsService,
        INotificationService notificationService, 
        IStatsService statsService,
        IWeaponService weaponService, DataContext context)
    {
        _userBatimentsService = userBatimentsService;
        _notificationService = notificationService;
        _statsService = statsService;
        _weaponService = weaponService;
        _context = context;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (model?.BatimentToUpgrade == null && model?.WeaponToUpgradeId == null)
            return (false, "Batiment et Arme à améliorer non spécifié");
        
        if (model is { BatimentToUpgrade: not null, WeaponToUpgradeId: not null })
            return (false, "Batiment et Arme à améliorer spécifiés");

        // une seule carte pour améliorer
        if (userCards.Count < 1)
            return (false, "Une carte ou plus est nécessaire pour améliorer un bâtiment !");

        // une des carte est déjà en action
        if (userCards.Any(uc => uc.Action != null))
            return (false, "Une des cartes est déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "laboratoire"))
            return (false, "Batiment déjà plein");
        
        // Stats et ressources suffisantes ?
        if ((model.BatimentToUpgrade != null && user.Nourriture < userCards.Count) 
            || (model.WeaponToUpgradeId != null && user.Nourriture < userCards.Count * 4))
            return (false, "Pas assez de nourritures");

        int creatiumPrice, intelPrice, forcePrice;

        if (model.BatimentToUpgrade != null)
        {
            var level = GetLevelOfUserBat(user.UserBatimentData, model);
            (creatiumPrice, intelPrice, forcePrice) =
                _userBatimentsService.GetBatimentPrices(level, model.BatimentToUpgrade);
        }
        else
        {
            var weapon = user.UserWeapons.FirstOrDefault(uw => uw.Id == model.WeaponToUpgradeId);
            if (weapon == null)
                return (false, "Arme non trouvée");
            
            // l'arme est déjà utilisée
            if (weapon.UserCard != null)
                return (false, "Arme déjà utilisée, déséquipez la avant de l'améliorer");

            
            var nbMachine = _context.Stats.FirstOrDefault(s => s.UserId == user.Id)?.TotalMachineUsed ?? 0;
            (creatiumPrice, intelPrice, forcePrice) = _weaponService.GetWeaponPrices(weapon.Power, user.UserWeapons.Count, nbMachine);
        }

        if (user.Creatium < creatiumPrice)
            return (false, "Pas assez de créatium => " + user.Creatium + " < " + creatiumPrice);
        
        var intelTotal = userCards.Sum(uc => uc.Competences.Intelligence);
        var forceTotal = userCards.Sum(uc => uc.Competences.Force);
        if (intelTotal < intelPrice)
            return (false, "Pas assez d'intelligence => " + intelTotal + " < " + intelPrice);
        
        if (forceTotal < forcePrice)
            return (false, "Pas assez de force => " + forceTotal + " < " + forcePrice);

        return (true, "");
    }

    public async Task<List<Action>> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
        var isABatUpgrade = model.BatimentToUpgrade != null;

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible d'améliorer le bâtiment : " + validation.why, 400);
        
        // set base value
        DateTime endTime;
        var intelTotal = userCards.Sum(uc => uc.Competences.Intelligence);
        int creatiumPrice;
        
        // bat upgrade
        if (isABatUpgrade)
        {
            var level = GetLevelOfUserBat(user.UserBatimentData, model);

            // calculate action end time with extra levels
            (creatiumPrice, var intelPrice, _) =
                _userBatimentsService.GetBatimentPrices(level, model.BatimentToUpgrade);
            var extraLevels = intelTotal - intelPrice;
            endTime = CalculateActionEndTime(level, extraLevels);
        }
        else // weapon upgrade
        {
            var weaponPower = user.UserWeapons.FirstOrDefault(uw => uw.Id == model.WeaponToUpgradeId)?.Power;
            if (weaponPower == null)
                throw new AppException("Arme non trouvée", 404);
            
            var nbMachine = _context.Stats.FirstOrDefault(s => s.UserId == user.Id)?.TotalMachineUsed ?? 0;
            (creatiumPrice,var intelPrice, _) =
                _weaponService.GetWeaponPrices((int)weaponPower, user.UserWeapons.Count, nbMachine);
            var extraLevels = intelTotal - intelPrice;
            endTime = CalculateActionEndTime((int)weaponPower, extraLevels, false);
        }

        var action = new ActionAmeliorer
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            BatimentToUpgrade = model.BatimentToUpgrade,
            WeaponToUpgradeId = model.WeaponToUpgradeId
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // consume resources
        
        user.Nourriture -= isABatUpgrade ? userCards.Count : userCards.Count * 4;
        user.Creatium -= creatiumPrice;
        
        await context.SaveChangesAsync();

        // return response
        return new List<Action> { action };
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
        var isABatUpgrade = model.BatimentToUpgrade != null;
        
        // validate action
        var validation = CanExecuteAction(user, userCards, model);

        // set base value
        DateTime endTime;
        var intelTotal = userCards.Sum(uc => uc.Competences.Intelligence);
        int creatiumPrice;
        int level;
        int finalExtraLevels = 0;
        
        // bat upgrade
        if (isABatUpgrade)
        {
            level = GetLevelOfUserBat(user.UserBatimentData, model);

            // calculate action end time with extra levels
            (creatiumPrice, var intelPrice, _) =
                _userBatimentsService.GetBatimentPrices(level, model.BatimentToUpgrade);
            var extraLevels = intelTotal - intelPrice;
            finalExtraLevels = extraLevels < 0 ? 0 : extraLevels;
            
            endTime = CalculateActionEndTime(level, finalExtraLevels);
        }
        else // weapon upgrade
        {
            var weaponPower = user.UserWeapons.FirstOrDefault(uw => uw.Id == model.WeaponToUpgradeId)?.Power;
            if (!weaponPower.HasValue)
                throw new AppException("Arme non trouvée", 404);
            level = weaponPower.Value * 3;
            
            var nbMachine = _context.Stats.FirstOrDefault(s => s.UserId == user.Id)?.TotalMachineUsed ?? 0;
            (creatiumPrice, var intelPrice, _) =
                _weaponService.GetWeaponPrices(weaponPower.Value, user.UserWeapons.Count, nbMachine);
            
            var extraLevels = intelTotal - intelPrice;
            finalExtraLevels = extraLevels < 0 ? 0 : extraLevels;
            endTime = CalculateActionEndTime(weaponPower.Value, finalExtraLevels, false);
        }

        // generates gains and couts strings
        var gain = new Dictionary<string, string>
        {
            { "Up intel", level + " fois random" },
            { isABatUpgrade ? "Up batiment" : "Up arme", 
                isABatUpgrade ? "1 fois" : "2 fois" },
            { "réduites", isABatUpgrade ? finalExtraLevels + "Heures" : finalExtraLevels * 30 + "Minutes" }
        };

        var couts = new Dictionary<string, string>
        {
            { "nourriture", isABatUpgrade ? userCards.Count.ToString() : (userCards.Count * 4).ToString() },
            { "créatium", creatiumPrice.ToString() }
        };
        
        var action = new ActionEstimationResponse
        {
            EndDates = new List<DateTime> { endTime },
            ActionName = "ameliorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = gain,
            Couts = couts,
            Error = !validation.valid ? "Impossible d'ameliorer : " + validation.why : null
        };

        // return response
        return action;
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionAmeliorer actionAmeliorer)
            throw new AppException("Action not found", 404);
        var isABatUpgrade = actionAmeliorer.BatimentToUpgrade != null;
        
        var niveauIntelADonner = isABatUpgrade ? EndBatimentUpgradeAction(actionAmeliorer, context) : EndWeaponUpgradeAction(actionAmeliorer, context);

        // up intel of cards
        // sort cards by intel and remove all that got are max 10 intel
        var userCardsToUp = actionAmeliorer.UserCards
            .OrderBy(uc => uc.Competences.Intelligence)  // Change OrderByDescending to OrderBy to start with the card with the lowest intel
            .Where(uc => uc.Competences.Intelligence < 10)
            .ToList();
        
        var cardNameForIntelUp = UpIntelOfCards(userCardsToUp, niveauIntelADonner);
        
        // notify user
        _notificationService.SendNotificationToUser(actionAmeliorer.User, new NotificationRequest(
            "Amélioration terminée", 
            $"Les cartes suivantes ont gagné des points d'intelligence : {string.Join(", ", cardNameForIntelUp.Select(kvp => $"{kvp.Key} (+{kvp.Value})"))}",
            "cards"),
            context);
        
        // stats
        if (isABatUpgrade) 
            _statsService.OnBuildingsUpgraded(actionAmeliorer.User.Id);
        else 
            _statsService.OnWeaponsUpgraded(actionAmeliorer.User.Id);
        
        // augment score
        actionAmeliorer.User.Score += 100;
        
        // remove action
        context.Actions.Remove(actionAmeliorer);

        return context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        context.Entry(user).Reference(u => u.UserBatimentData).Load();
        
        if (action is not ActionAmeliorer actionAmeliorer)
            throw new AppException("Action not found", 404);
        var isABatUpgrade = actionAmeliorer.BatimentToUpgrade != null;
        
        // refund resources
        int nourriturePrice;
        int creatiumPrice;
        
        if (isABatUpgrade)
        {
            var level = GetLevelOfUserBat(user.UserBatimentData, new ActionRequest { BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade });
            (creatiumPrice, _, _) = _userBatimentsService.GetBatimentPrices(level, actionAmeliorer.BatimentToUpgrade);
            nourriturePrice = actionAmeliorer.UserCards.Count;
        }
        else
        {
            context.Entry(user).Collection(u => u.UserWeapons).Load();
            var weaponPower = user.UserWeapons.FirstOrDefault(uw => uw.Id == actionAmeliorer.WeaponToUpgradeId)?.Power;
            if (weaponPower == null)
                throw new AppException("Arme non trouvée", 404);
            
            var nbMachine = _context.Stats.FirstOrDefault(s => s.UserId == user.Id)?.TotalMachineUsed ?? 0;
            (creatiumPrice, _, _) = _weaponService.GetWeaponPrices((int)weaponPower, user.UserWeapons.Count, nbMachine);
            nourriturePrice = actionAmeliorer.UserCards.Count * 4;
        }
        
        user.Nourriture += nourriturePrice;
        user.Creatium += creatiumPrice;

        context.Actions.Remove(actionAmeliorer);

        return context.SaveChangesAsync();
    }

    // Helpers
    private int EndWeaponUpgradeAction(ActionAmeliorer actionAmeliorer, DataContext context)
    {
        var user = actionAmeliorer.User;
        context.Entry(user).Collection(u => u.UserWeapons).Load();
        var weapon = user.UserWeapons.FirstOrDefault(uw => uw.Id == actionAmeliorer.WeaponToUpgradeId);
        if (weapon == null)
            throw new AppException("Arme non trouvée", 404);
        
        // load Weapon entity
        context.Entry(weapon).Reference(w => w.Weapon).Load();
        
        // up weapon power
        weapon.Power += 2;
        
        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest(
                "Amélioration terminée", 
                $"Votre arme {weapon.Weapon.Name} a été amélioré !\r\nElle est maintenant de niveau {weapon.Power} !", 
                "oneweapon", weapon.Id), 
            context);
        
        return weapon.Power-1 * 3;
    }
    
    private int EndBatimentUpgradeAction(ActionAmeliorer actionAmeliorer, DataContext context)
    {
        var userBatimentData = _userBatimentsService.GetOrCreateUserBatimentData(actionAmeliorer.User, context);

        var niveauIntelADonner = GetLevelOfUserBat(userBatimentData, new ActionRequest { BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade });

        // up batiment level
        switch (actionAmeliorer.BatimentToUpgrade)
        {
            case "cuisine":
                userBatimentData.CuisineLevel++;
                break;
            case "salledesport":
                userBatimentData.SalleSportLevel++;
                break;
            case "spatioport":
                userBatimentData.SpatioPortLevel++;
                break;
            case "satellite":
                userBatimentData.SatelliteLevel++;
                break;
            default:
                throw new AppException("Bâtiment non reconnu", 400);
        }

        // notify user
        _notificationService.SendNotificationToUser(actionAmeliorer.User, new NotificationRequest(
                "Amélioration terminée", 
                $"Votre bâtiment {actionAmeliorer.BatimentToUpgrade} a été amélioré !", 
                "buildings"), 
            context);
        
        return niveauIntelADonner;
    }
    
    private Dictionary<string, int> UpIntelOfCards(List<UserCard> userCardsToUp, int niveauIntelADonner)
    {
        var cardNameForIntelUp = new Dictionary<string, int>();
        
        while (niveauIntelADonner > 0 && userCardsToUp.Any(uc => uc.Competences.Intelligence < 10))
        {
            foreach (var card in userCardsToUp
                         .Where(card => niveauIntelADonner > 0 &&
                                        card.Competences.Intelligence < 10))
            {
                card.Competences.Intelligence++;
                niveauIntelADonner--;
                
                // add card name to dictionary for notification
                if (cardNameForIntelUp.ContainsKey(card.Card.Name))
                    cardNameForIntelUp[card.Card.Name]++;
                else
                    cardNameForIntelUp.Add(card.Card.Name, 1);
            }
        }
        
        return cardNameForIntelUp;
    }
    
    private DateTime CalculateActionEndTime(int level, int extraLevels, bool isBatUpgrade = true)
    {
        if (level <= 0) level = 1;
        
        var hours = isBatUpgrade ? 12 * level - extraLevels : 16 * level - 0.5 * extraLevels;
        if (hours < 1) hours = 0;
        
        return DateTime.Now.AddHours(hours);
        //return DateTime.Now.AddSeconds(hours);
    }
    
    private static int GetLevelOfUserBat(UserBatimentData batimentData, ActionRequest model)
    {
        // get batiment requested level (BatimentToUpgrade)
        var level = model.BatimentToUpgrade switch
        {
            "cuisine" => batimentData.CuisineLevel,
            "salledesport" => batimentData.SalleSportLevel,
            "spatioport" => batimentData.SpatioPortLevel,
            "satellite" => batimentData.SatelliteLevel,
            _ => throw new AppException("Bâtiment non reconnu", 400)
        };
        return level;
    }
}