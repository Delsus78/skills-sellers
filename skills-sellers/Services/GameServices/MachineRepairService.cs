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
    private readonly IActionTaskService _actionTaskService;
    private readonly IStatsService _statsService;
    private readonly IWeaponService _weaponService;
    
    public MachineRepairService(DataContext context, 
        IActionTaskService actionTaskService,
        IStatsService statsService, 
        IWeaponService weaponService)
    {
        _context = context;
        _actionTaskService = actionTaskService;
        _statsService = statsService;
        _weaponService = weaponService;
    }
    
    public GamesResponse GetGameOfTheDay(int userId)
    {
        var nbCards = _context.UserCards.Count(uc => uc.UserId == userId);
        var (creatiumPrice, orPrice) = _weaponService.GetWeaponConstructionPrice(nbCards);
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
            IsRepairing = _context.Actions.Any(a => a is ActionReparer && userId == a.UserId),
            CreatiumPrice = creatiumPrice,
            OrPrice = orPrice
        };
    }

    public async Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        // start action
        _ = await _actionTaskService.CreateNewActionAsync(user.Id, new ActionRequest
        {
            ActionName = "reparer",
            CardsIds = model.CardsIds
        });
        
        return new GamesPlayResponse
        {
            Name = "MACHINE"
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        return new GamesPlayResponse
        {
            Name = "MACHINE",
            Error = "L'estimation est obselète depuis la 2.0, veuillez utiliser l'endpoint Estimation d'Action"
        };
    }
}