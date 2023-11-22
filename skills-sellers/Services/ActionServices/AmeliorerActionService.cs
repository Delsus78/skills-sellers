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
    private readonly IStatsService _statsService;

    public AmeliorerActionService(
        IUserBatimentsService userBatimentsService,
        INotificationService notificationService, 
        IStatsService statsService)
    {
        _userBatimentsService = userBatimentsService;
        _notificationService = notificationService;
        _statsService = statsService;
    }
    
    public (bool valid, string why) CanExecuteAction(User user, List<UserCard> userCards, ActionRequest? model)
    {
        if (model?.BatimentToUpgrade == null)
            return (false, "Batiment à améliorer non spécifié");

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
        if (user.Nourriture < userCards.Count)
            return (false, "Pas assez de nourritures");
        
        var level = GetLevelOfUserBat(user.UserBatimentData, model);

        (int creatiumPrice, int intelPrice, int forcePrice) = _userBatimentsService.GetBatimentPrices(level);
        
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

        // validate action
        var validation = CanExecuteAction(user, userCards, model);
        if (!validation.valid)
            throw new AppException("Impossible d'améliorer le bâtiment : " + validation.why, 400);
        
        // get user bat level
        var level = GetLevelOfUserBat(user.UserBatimentData, model);
        
        // calculate action end time with extra levels
        var intelTotal = userCards.Sum(uc => uc.Competences.Intelligence);
        var extraLevels = intelTotal - _userBatimentsService.GetBatimentPrices(level).intelPrice;
        var endTime = CalculateActionEndTime(level, extraLevels);

        var action = new ActionAmeliorer
        {
            UserCards = userCards,
            DueDate = endTime,
            User = user,
            BatimentToUpgrade = model.BatimentToUpgrade
        };
        
        // actualise bdd
        await context.Actions.AddAsync(action);
        
        // consume resources
        var (creatiumPrice, _, _) = _userBatimentsService.GetBatimentPrices(level);
        
        user.Nourriture -= userCards.Count;
        user.Creatium -= creatiumPrice;
        
        await context.SaveChangesAsync();

        // return response
        return new List<Action> { action };
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, model);

        // get user bat level
        var level = GetLevelOfUserBat(user.UserBatimentData, model);
        
        // calculate action end time and resources
        var (creatiumPrice, intelPrice, _) = _userBatimentsService.GetBatimentPrices(level);
        var extraLevels = userCards.Sum(uc => uc.Competences.Intelligence) - intelPrice;
        var finalExtraLevels = extraLevels < 0 ? 0 : extraLevels;
        var endTime = CalculateActionEndTime(level, finalExtraLevels);
        
        var action = new ActionEstimationResponse
        {
            EndDates = new List<DateTime> { endTime },
            ActionName = "ameliorer",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "Up intel", level + " fois random" },
                { "Up lvl bâtiment", "+1"},
                { "Heures réduites", finalExtraLevels.ToString()}
            },
            Couts = new Dictionary<string, string>
            {
                { "nourriture", userCards.Count.ToString() },
                { "créatium", creatiumPrice.ToString() }
            },
            Error = !validation.valid ? "Impossible d'ameliorer le batiment : " + validation.why : null
        };

        // return response
        return action;
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        if (action is not ActionAmeliorer actionAmeliorer)
            throw new AppException("Action not found", 404);
        
        // sort cards by intel and remove all that got are max 10 intel
        var userCards = actionAmeliorer.UserCards
            .OrderBy(uc => uc.Competences.Intelligence)  // Change OrderByDescending to OrderBy to start with the card with the lowest intel
            .Where(uc => uc.Competences.Intelligence < 10)
            .ToList();
        
        var userBatimentData = _userBatimentsService.GetOrCreateUserBatimentData(actionAmeliorer.User, context);

        var niveauIntelADonner = GetLevelOfUserBat(userBatimentData, new ActionRequest { BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade });

        var cardNameForIntelUp = new Dictionary<string, int>();
        
        while (niveauIntelADonner > 0 && userCards.Any(uc => uc.Competences.Intelligence < 10))
        {
            foreach (var card in userCards
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
        
        // notify user
        _notificationService.SendNotificationToUser(actionAmeliorer.User, new NotificationRequest(
            "Amélioration terminée", 
            $"Les cartes suivantes ont gagné des points d'intelligence : {string.Join(", ", cardNameForIntelUp.Select(kvp => $"{kvp.Key} (+{kvp.Value})"))}"), 
            context);

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
            default:
                throw new AppException("Bâtiment non reconnu", 400);
        }
        
        // stats
        _statsService.OnBuildingsUpgraded(actionAmeliorer.User.Id);
        
        // remove action
        context.Actions.Remove(actionAmeliorer);

        // notify user
        _notificationService.SendNotificationToUser(actionAmeliorer.User, new NotificationRequest(
            "Amélioration terminée", 
            $"Votre bâtiment {actionAmeliorer.BatimentToUpgrade} a été amélioré !"), 
            context);

        return context.SaveChangesAsync();
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        context.Entry(user).Reference(u => u.UserBatimentData).LoadAsync();
        
        if (action is not ActionAmeliorer actionAmeliorer)
            throw new AppException("Action not found", 404);

        // refund resources
        var level = GetLevelOfUserBat(user.UserBatimentData, new ActionRequest { BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade });
        var (creatiumPrice, _, _) = _userBatimentsService.GetBatimentPrices(level);
        user.Nourriture += actionAmeliorer.UserCards.Count;
        user.Creatium += creatiumPrice;

        context.Actions.Remove(actionAmeliorer);

        return context.SaveChangesAsync();
    }

    // Helpers

    private DateTime CalculateActionEndTime(int level, int extraLevels)
    {
        var hours = 12 * level - extraLevels;
        return DateTime.Now.AddHours(hours);
    }
    
    private static int GetLevelOfUserBat(UserBatimentData batimentData, ActionRequest model)
    {
        // get batiment requested level (BatimentToUpgrade)
        var level = model.BatimentToUpgrade switch
        {
            "cuisine" => batimentData.CuisineLevel,
            "salledesport" => batimentData.SalleSportLevel,
            "spatioport" => batimentData.SpatioPortLevel,
            _ => throw new AppException("Bâtiment non reconnu", 400)
        };
        return level;
    }
}