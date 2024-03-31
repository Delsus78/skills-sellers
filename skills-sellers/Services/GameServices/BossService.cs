using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services.GameServices;

public class BossService : IGameService
{
    private readonly ICardService _cardService;
    private readonly DataContext _context;
    private readonly IActionTaskService _actionTaskService;
    private readonly INotificationService _notificationService;

    public BossService(
        ICardService cardService, 
        DataContext context, 
        IActionTaskService actionTaskService, 
        INotificationService notificationService)
    {
        _cardService = cardService;
        _context = context;
        _actionTaskService = actionTaskService;
        _notificationService = notificationService;
    }

    public GamesResponse GetGameOfTheDay(int _)
    {
        var seed =  DateTime.Today.DayOfYear + DateTime.Today.Year;
        // get a random card
        var bossCardType = _cardService.GetRandomCard(seed);
        
        // unfollow card from context
        _context.Entry(bossCardType).State = EntityState.Detached;

        #region Random Competences

        var totalPower = _context.UserCards.Count() * 3;
        var ptsLeft = totalPower;
        var stillAvailable = new List<string> { "force", "intel", "cuisine", "charisme", "exploration" };
        var valuesDicto = new Dictionary<string, int>
        {
            { "force", 0 },
            { "intel", 0 },
            { "cuisine", 0 },
            { "charisme", 0 },
            { "exploration", 0 }
        };

        var rdm = Randomizer.Random(seed);
        
        while (ptsLeft > 0)
        {
            var index = rdm.Next(0, 5);
            var key = stillAvailable[index];
            
            valuesDicto[key] += 1;
            ptsLeft -= 1;
        }

        var bossCompetences = new Competences
        {
            Charisme = valuesDicto["charisme"],
            Cuisine = valuesDicto["cuisine"],
            Exploration = valuesDicto["exploration"],
            Force = valuesDicto["force"],
            Intelligence = valuesDicto["intel"]
        };
        
        // take the biggest competence
        var max = valuesDicto.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        bossCardType.Name += max switch
        {
            "force" => " Le Guerrier",
            "intel" => " Le Savant",
            "cuisine" => " Le Cuisinier",
            "charisme" => " Le Charismatique",
            "exploration" => " L'Explorateur",
            _ => "Le Boss"
        };

        #endregion
        
        var bossCard = new UserCard
        {
            Card = bossCardType,
            Competences = bossCompetences,
            UserWeapon = new UserWeapon
            {
                Power = Randomizer.RandomInt(1, 40, seed),
                Weapon = new Weapon
                {
                    Name = "Poing",
                    Description = "Poing."
                },
                Affinity = Randomizer.RandomWeaponAffinity(seed)
            },
            Action = new ActionGuerre()
        };
        
        // create a date time set to 18h of the current day
        var date = DateTime.Today.AddHours(18);
        
        if (DateTime.Now > date) 
            return new GamesBossResponse
            {
                Name = "boss - Terminé",
                Description = "Unissez vos forces pour vaincre le boss !\nRemportez 2 récompenses de guerre !",
                Regles = new Dictionary<string, string>(),
                EndDate = date,
                StartDate =  DateTime.Today,
                BossCard = bossCard.ToResponse()
            };
        
        // create the action if not started
        var actionBossResponse = StartActionIfNotStarted(date);
        
        return new GamesBossResponse
        {
            Name = "boss",
            Description = "Unissez vos forces pour vaincre le boss !\nRemportez 2 récompenses de guerre !",
            Regles = new Dictionary<string, string>(),
            EndDate = date,
            StartDate =  DateTime.Today,
            BossCard = bossCard.ToResponse(),
            PlayersPower = actionBossResponse.PlayersPower
        };
    }

    public async Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        var action = _context.Actions
            .FirstOrDefault(a => a is ActionBoss);
        
        if (action is not ActionBoss)
        {
            GetGameOfTheDay(user.Id);
            action = _context.Actions.FirstOrDefault(a => a is ActionBoss);
        }
        
        
        // estimate and throw if not valid
        var response = EstimateGameOfTheDay(user, model);
        if (!string.IsNullOrEmpty(response.Error))
            throw new AppException(response.Error, 400);

        var cards = _context.Entry(user).Collection(u => u.UserCards).Query()
            .Where(uc => model.CardsIds.Contains(uc.CardId))
            .ToList();

        action.UserCards.AddRange(cards);

        // notify all players
        await _notificationService.SendNotificationToAll(new NotificationRequest("BOSSFIGHT",
            $"{user.Pseudo} ajoute des cartes contre le boss !", "cards"), _context);
        
        await _context.SaveChangesAsync();
        
        return new GamesPlayResponse
        {
            Name = "boss"
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        // is cards estimation valid
        var actionEstimation = _actionTaskService.EstimateAction(user.Id, new ActionRequest
        {
            ActionName = "boss",
            CardsIds = model.CardsIds
        });
        
        return new GamesPlayResponse
        {
            Name = "boss",
            Error = actionEstimation.Error ?? ""
        };
    }
    
    private ActionBossResponse StartActionIfNotStarted(DateTime endTime)
    {
        var action = _context.Actions
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.Competences)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.UserWeapon)
            .Include(a => a.UserCards)
            .ThenInclude(uc => uc.User)
            .FirstOrDefault(a => a is ActionBoss);
        
        if (action is not ActionBoss)
        {
            // start the action
            _ = _actionTaskService.CreateNewActionAsync(-1, new ActionRequest
            {
                ActionName = "boss",
                CardsIds = new List<int>(),
                DueDate = endTime
            });
        }
        
        // get all power by playerNames
        var powerByPlayer = action?.UserCards
            .GroupBy(uc => uc.User.Pseudo)
            .ToDictionary(g => g.Key, g => g.Sum(uc => uc.ToResponse().Power));
        
        return new ActionBossResponse
        {
            EndTime = endTime,
            PlayersPower = powerByPlayer ?? new Dictionary<string, int>()
        };
    }

    public async Task<GamesPlayResponse> CancelPlayOfTheDay(User user, GamesRequest model)
    {
        var action = _context.Actions
            .FirstOrDefault(a => a is ActionBoss);
        
        if (action is not ActionBoss)
        {
            GetGameOfTheDay(user.Id);
            action = _context.Actions.FirstOrDefault(a => a is ActionBoss);
        }
        
        // check if user has cards in action
        var userCards = _context.Entry(user).Collection(u => u.UserCards).Query()
            .Where(uc => model.CardsIds.Contains(uc.CardId))
            .ToList();
        
        // remove card from action
        action.UserCards.RemoveAll(uc => userCards.Contains(uc));
        
        await _context.SaveChangesAsync();
        
        return new GamesPlayResponse
        {
            Name = "boss"
        };
    }
}