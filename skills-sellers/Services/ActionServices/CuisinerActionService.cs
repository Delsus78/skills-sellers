using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

namespace skills_sellers.Services.ActionServices;

public class CuisinerActionService : IActionService
{
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly INotificationService _notificationService;
    private readonly IStatsService _statsService;
    
    public CuisinerActionService(
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
        // une seule carte pour cuisiner
        if (userCards.Count != 1)
            return (false, "Une seule carte pour cuisiner");
        var userCard = userCards.First();
        
        // Carte déjà en action
        if (userCard.Action != null)
            return (false, "Carte déjà en action");
        
        // Batiment déjà plein
        if (_userBatimentsService.IsUserBatimentFull(user, "cuisine"))
            return (false, "Batiment déjà plein, attendez demain !");

        return (true, "");
    }

    public async Task<Action> StartAction(User user, ActionRequest model, DataContext context, IServiceProvider serviceProvider)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        if (!validation.valid)
            throw new AppException("Impossible de cuisiner : " + validation.why, 400);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();
        
        // Random plat
        var randomPlat = Randomizer.RandomPlat();
        
        var action = new ActionCuisiner
        {
            UserCards = userCards,
            DueDate = endTime,
            Plat = randomPlat,
            User = user
        };
        
        // actualise bdd and nb cuisine used today
        user.UserBatimentData.NbCuisineUsedToday += 1;
        
        await context.Actions.AddAsync(action);
        await context.SaveChangesAsync();

        // return response
        return action;
    }

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        var userCards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();

        // validate action
        var validation = CanExecuteAction(user, userCards, null);
        
        // calculate action end time
        var endTime = CalculateActionEndTime();

        var action = new ActionEstimationResponse
        {
            EndTime = endTime,
            ActionName = "cuisiner",
            Cards = userCards.Select(uc => uc.ToResponse()).ToList(),
            Gains = new Dictionary<string, string>
            {
                { "nourriture", (userCards.First().Competences.Cuisine).ToString() },
                { "Up cuisine", "20%" }
            },
            Error = !validation.valid ? "Impossible de cuisiner : " + validation.why : null
        };
        
        // return response
        return action;
    }

    public Task DeleteAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        // get user linked to action
        var user = action.User;

        // actualise bdd and nb cuisine used today
        user.UserBatimentData.NbCuisineUsedToday -= 1;
        context.Actions.Remove(action);
        
        return context.SaveChangesAsync();
    }

    public Task EndAction(Action action, DataContext context, IServiceProvider serviceProvider)
    {
        var user = action.User;
        
        if (action is not ActionCuisiner actionCuisiner)
            throw new AppException("Action not found", 404);
        
        // only one card
        var userCard = actionCuisiner.UserCards.First();

        #region REWARDS

        // give nourriture
        var amount = userCard.Competences.Cuisine;
        user.Nourriture += amount;
        
        // stats
        _statsService.OnMealCooked(user.Id);

        // chance to up cuisine competence
        // - 20% de chance de up
        if (Randomizer.RandomPourcentageUp() && userCard.Competences.Cuisine < 10)
        {
            userCard.Competences.Cuisine += 1;
            // notify user
            _notificationService.SendNotificationToUser(user, new NotificationRequest
            (
                "Compétence cuisine",
                $"Votre carte {userCard.Card.Name} a gagné 1 point de compétence en cuisine !"
            ), context);
        }
        
        #endregion

        // remove action
        context.Actions.Remove(actionCuisiner);

        // notify user
        _notificationService.SendNotificationToUser(user, new NotificationRequest
        (
            "Cuisiner",
            $"Votre carte {userCard.Card.Name} a cuisiné {amount} nourriture avec son plat {actionCuisiner.Plat} !"
        ), context);
        
        return context.SaveChangesAsync();
    }

    // Helpers

    private DateTime CalculateActionEndTime()
    {
        return DateTime.Now.AddMinutes(1);
    }
}