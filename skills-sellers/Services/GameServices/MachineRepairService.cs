using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services.GameServices;

public class MachineRepairService : IGameService
{
    private readonly DataContext _context;
    private readonly IActionService<ActionReparer> _reparerActionService;
    private readonly IStatsService _statsService;
    
    public MachineRepairService(DataContext context, 
        IActionService<ActionReparer> reparerActionService,
        IStatsService statsService)
    {
        _context = context;
        _reparerActionService = reparerActionService;
        _statsService = statsService;
    }
    
    public GamesResponse GetGameOfTheDay(int userId)
    {
        return new GamesMachineResponse
        {
            Name = "MACHINE",
            Description = "Bonjour bonjour! Excusez-moi de vous déranger, mais j'ai un petit soucis avec ma machine ᒲᔑᓵ⍑╎リᒷ ! " +
                          "Je ne sais pas ce qu'il se passe, mais elle ne fonctionne plus. " +
                          "Avez-vous des gens qualifiés pour réparer ma machine? " +
                          "En échange je vous laisserai utiliser ma machine! Avec une réduction de 50% sur la première utilisation!",
            Regles = new Dictionary<string, string>
            {
                {
                    "Vous devez réparer la machine pour pouvoir l'utiliser.",
                    "Puis 1000 d'or sans réduction de coût par utilisation."
                },
                {
                    "Vous pouvez déposer le nombre de cartes que vous voulez",
                    "Ils seront indisponible pendant 1h"
                },
                {
                    "Votre probabilité de réparer la machine est calculé de la façon suivante :",
                    "Somme INTEL des cartes déposées / Nombre TOTAL des cartes de votre collection * 4"
                }
            },
            IsRepairing = _context.Actions.Any(a => a is ActionReparer && userId == a.UserId)
        };
    }

    public async Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        if (model.CardsIds.Count == 0 && model.Bet > 0) // play action
        {
            var (valid, error) = CanPlayGameOfTheDay(user, model);
            if (!valid)
                throw new AppException(error, 400);
            
            // stats
            _statsService.OnMachineUsed(user.Id);
            
            // promo ?
            if (user.StatRepairedObjectMachine > 0) // no promo
            {
                user.Or -= model.Bet;
            }
            else // promo
            {
                user.Or -= model.Bet / 2;
            }

            user.NbCardOpeningAvailable++;
            user.StatRepairedObjectMachine++;
            _context.Users.Update(user);
            
            await _context.SaveChangesAsync();
        } 
        else if (model.CardsIds.Count > 0 && model.Bet == 0) // repair action
        {
            var cards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
            var (valid, error) = CanRepair(cards, user.Id);
            if (!valid)
                throw new AppException(error, 400);
            
            // set cards in action
            var totalIntel = cards.Sum(c => c.Competences.Intelligence);
            var chances = CalculateRepairChances(totalIntel, user.UserCards.Count);
            
            await _reparerActionService.StartAction(user, new ActionRequest
            {
                CardsIds = model.CardsIds,
                RepairChances = chances
            });
        }
        
        return new GamesPlayResponse
        {
            Name = "MACHINE"
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        // Une estimation est effectué pour savoir si le joueur peut réparer la machine, et quelle est sa probabilité de réussite
        // Une estimation est aussi effectué pour savoir si le joueur peut payer pour utiliser la machine

        switch (model.CardsIds.Count)
        {
            // play action
            case 0 when model.Bet > 0:
            {
                var (valid, error) = CanPlayGameOfTheDay(user, model);
            
                if (!valid)
                    throw new AppException(error, 400);
            
                return new GamesPlayResponse
                {
                    Name = "MACHINE"
                };
            }
            // repair action
            case > 0 when model.Bet == 0:
            {
                var cards = user.UserCards.Where(uc => model.CardsIds.Contains(uc.CardId)).ToList();
                var (valid, error) = CanRepair(cards, user.Id);
                if (!valid)
                    throw new AppException(error, 400);
            
                // set cards in action
                var totalIntel = cards.Sum(c => c.Competences.Intelligence);
                var chances = CalculateRepairChances(totalIntel, user.UserCards.Count);
            
                return new GamesPlayResponse
                {
                    Name = "MACHINE",
                    Chances = chances
                };
            }
            default:
                throw new AppException("Vous devez jouer une carte minimum.", 400);
        }
    }

    #region HELPERS METHODS
    private (bool valid, string error) CanPlayGameOfTheDay(User user, GamesRequest model)
    {
        if (user.Or < model.Bet)
            return (false, "Vous n'avez pas assez d'or !");

        return user.StatRepairedObjectMachine switch
        {
            -1 => (false, "Vous devez réparer la machine pour pouvoir l'utiliser !"),
            0 when model.Bet != 500 => (false, "Vous devez payer 500 d'or pour utiliser la machine la première fois !"),
            > 0 when model.Bet != 1000 => (false, "Vous devez payer 1000 d'or pour utiliser la machine !"),
            _ => (true, "")
        };
    }

    private (bool valid, string error) CanRepair(List<UserCard> cards, int userId)
    {
        // 1 card minimum
        if (cards.Count < 1)
            return (false, "Vous devez jouer une carte minimum !");
        
        // cards already in action ?
        if (cards.Any(c => c.Action != null))
            return (false, "Une de vos cartes est déjà en action !");
        
        // user already repairing ?
        if (_context.Actions.Any(a => a is ActionReparer && a.UserId == userId))
            return (false, "Vous êtes déjà en train de réparer la machine !");
        
        return (true, "");
    }

    private double CalculateRepairChances(int totalIntel, int totalCards)
    {
        return totalIntel / ((double)totalCards * 4) * 100;
    }

    #endregion
}