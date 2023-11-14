using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services.GameServices;

public class CasinoService : IGameService
{
    private readonly DataContext _context;
    private readonly IStatsService _statsService;
    
    public CasinoService(DataContext context, IStatsService statsService)
    {
        _context = context;
        _statsService = statsService;
    }
    
    public GamesResponse GetGameOfTheDay(int userId)
    {
        return new GamesResponse
        {
            Name = "CASINO",
            Description = "L'argent rend heureux, c'est bien connu. Mais l'argent rend aussi beau. " +
                          "Et c'est ce que vous allez pouvoir constater en jouant au casino. " +
                          "Plus vous misez de l'or, plus vous avez de chance d'augmenter le charisme de votre personnage.",
            Regles = new Dictionary<string, string>
            {
                { "Plus vous misez de l'or, plus vous avez de chance d'augmenter le charisme de la carte jouée !", "" },
                { "Vous pouvez miser jusqu'à 1000 pièces d'or.", "" }
            }
        };
    }

    public Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        var gamePlay = EstimateGameOfTheDay(user, model);
        
        // remove gold
        user.Or -= model.Bet;
        
        // try to win
        var rand = new Random();
        var result = rand.NextDouble() * 100;
        if ( result > gamePlay.Chances)
        {
            // lose
            _context.SaveChanges();
            
            // stats
            _statsService.OnLooseGoldAtCharismeCasino(user.Id, model.Bet);
            
            return Task.FromResult(new GamesPlayResponse
            {
                Name = "CASINO",
                Chances = gamePlay.Chances,
                Results = result,
                Win = false
            });
        }
        
        // win
        var card = user.UserCards.FirstOrDefault(c => c.Card.Id == model.CardsIds[0]);
        
        card.Competences.Charisme += 1;

        // save stats total win at casino
        _statsService.OnWinAtCharismeCasino(user.Id);

        // save
        _context.SaveChanges();
        
        return Task.FromResult(new GamesPlayResponse
        {
            Name = "CASINO",
            Chances = gamePlay.Chances,
            Results = result,
            Win = true
        });
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        if (model.CardsIds.Count != 1)
            throw new AppException("Vous devez jouer une seule carte !", 400);
        
        // get entity card
        var card = user.UserCards.FirstOrDefault(c => c.Card.Id == model.CardsIds[0]);
        var validPlay = CanPlayGameOfTheDay(user, model, card);

        if (!validPlay.valid)
            throw new AppException(validPlay.error, 400);
                
        var chances = Math.Min(CalculateWinChance(model.Bet, card.Competences.Charisme), 100);

        // calculer le pourcentage de chance de gagner
        // Gain = pourcentage
        // 10% = 100 pieces d'or 
        return new GamesPlayResponse
        {
            Name = "CASINO",
            Chances = chances
        };

    }

    #region HELPERS METHODS
    
    private (bool valid, string error) CanPlayGameOfTheDay(User user, GamesRequest model, UserCard? card)
    {
        // Name is not CASINO
        if (model.Name.ToLower() != "casino")
            return (false, "Le jeu demandé n'est pas disponible aujourd'hui.");
        
        // card not found
        if (card == null)
            return (false, "Vous n'avez pas cette carte !");
        
        // enought gold
        if (user.Or < model.Bet)
            return (false, "Vous n'avez pas assez d'or !");

        switch (model.Bet)
        {
            // bet > 0
            case <= 0:
                return (false, "Vous devez miser au moins 1 pièce d'or !");
            // bet <= 1000
            case > 1000:
                return (false, "Vous ne pouvez pas miser plus de 1000 pièces d'or !");
        }

        // cardsIds length != 1
        if (model.CardsIds.Count != 1)
            return (false, "Vous devez jouer une seule carte !");

        // card already got 10 charisme
        if (card.Competences.Charisme >= 8)
            return (false, "Cette carte a déjà atteint son charisme maximum (8 pour le casino)!");
        
        return (true, "");
    }

    private double CalculateWinChance(double mise, double charisme)
    {
        var baseChance = mise * 0.1;
        var difficultyFactor = Math.Pow(3, charisme / 9);

        var winChance = baseChance / difficultyFactor;

        return winChance;
    }


    #endregion
}