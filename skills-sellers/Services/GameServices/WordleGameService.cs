using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services.GameServices;

public class WordleGameService : IGameService
{
    private readonly DataContext _context;
    private readonly IStatsService _statsService;
    private readonly INotificationService _notificationService;

    public WordleGameService(DataContext context, IStatsService statsService, INotificationService notificationService)
    {
        _context = context;
        _statsService = statsService;
        _notificationService = notificationService;
    }

    private string GetRandomWord()
    {
        Random random = new(DateTime.Today.DayOfYear);
        
        // take all cards name, split them by space, and take a random word
        var allCardsWords = _context.Cards.GetAllCardNameWord();
        var res = allCardsWords[random.Next(0, allCardsWords.Count)];
        
        return res;
    }
    
    private List<ReplaceTuple> GetWordLetters(string word)
    {
        var wordOfTheDay = GetRandomWord();

        var validatedList = WordleAlgorithm.Wordle(word, wordOfTheDay.ToUpper());
        
        var res = new List<ReplaceTuple>();

        // for each letter, set 1, 0 or -1 if :
        // 1 : correct
        // 0 : present
        // -1 : absent
        for (var i = 0; i < word.Length; i++)
        {
            var letter = word[i];
            var status = validatedList[i];
            
            res.Add((letter, status switch
            {
                "correct" => 1,
                "present" => 0,
                "absent" => -1,
                _ => throw new AppException("Erreur lors de la validation du mot !", 500)
            }));
        }
        
        return res;
    }

    private List<List<ReplaceTuple>> RetrieveWords(int userId)
    {
        var wordleData = GetWordleGame(userId);
        
        return wordleData.Words.Select(GetWordLetters).ToList();
    }

    private WordleGame GetWordleGame(int userId)
    {
        var wordleData = _context.WordleGames
            .FirstOrDefault(w => w.UserId == userId);
        
        if (wordleData == null)
        {
            wordleData = new WordleGame
            {
                UserId = userId,
                GameDate = DateTime.Today,
                Win = null,
                Words = new List<string>()
            };
            
            // add or update
            _context.WordleGames.Add(wordleData);
            _context.SaveChanges();
        } 
        else if (wordleData.GameDate != DateTime.Today)
        {
            wordleData.Words = new List<string>();
            wordleData.Win = null;
            wordleData.GameDate = DateTime.Today;
            
            _context.WordleGames.Update(wordleData);
            _context.SaveChanges();
        }
        
        
        return wordleData;
    }

    public GamesResponse GetGameOfTheDay(int userId)
    {
        return new GamesWordleResponse
        {
            Name = "WORDLE",
            Description = "Trouvez le mot mystère en 5 essais maximum! " +
                          "Le mot est directement tiré au hasard parmis les titres des cartes!",
            Regles = new Dictionary<string, string>
            {
                { 
                    "Vous devez trouver le mot mystère en 5 essais maximum!", 
                    "Le mot est directement tiré au hasard parmis les titres des cartes!"
                }
            },
            Words = RetrieveWords(userId),
            NbLetters = GetRandomWord().Length,
            IsWin = GetWordleGame(userId).Win ?? false
        };
    }

    public async Task<GamesPlayResponse> PlayGameOfTheDay(User user, GamesRequest model)
    {
        var wordleData = GetWordleGame(user.Id);
        var wordOfTheDay = GetRandomWord();
        var word = model.Word?.ToUpper();
        
        if (wordleData.Words.Count >= 5)
            throw new AppException("Vous avez déjà joué 5 fois aujourd'hui !", 400);
        
        if (string.IsNullOrEmpty(word))
            throw new AppException("Vous devez entrer un mot!", 400);
        
        if (wordleData.Words.Contains(word))
            throw new AppException("Vous avez déjà essayé ce mot !", 400);

        if (_context.Cards.GetAllCardNameWord().All(w => w.ToUpper() != word))
            throw new AppException("Le mot n'existe pas !", 400);
        
        wordleData.Words.Add(word);
        
        // check if word is the word of the day
        var isWordOfTheDay = string.Equals(word, wordOfTheDay, StringComparison.CurrentCultureIgnoreCase);

        if (isWordOfTheDay)
        {
            wordleData.Win = true;
            user.NbCardOpeningAvailable++;
            
            // stats
            _statsService.OnMachineUsed(user.Id);
            
            // notify player
            await _notificationService.SendNotificationToUser(user, new NotificationRequest(
                    "WORDLE WIN", 
                    $"Vous avez gagné 1 ouverture de carte !"), 
                _context);
        }
        else if (wordleData.Words.Count >= 5)
        {
            wordleData.Win = false;
        }

        await _context.SaveChangesAsync();
        
        return new GamesPlayResponse
        {
            Name = "WORDLE",
            Words = RetrieveWords(user.Id),
            Win = isWordOfTheDay
        };
    }

    public GamesPlayResponse EstimateGameOfTheDay(User user, GamesRequest model)
    {
        throw new AppException("Ce jeu n'a pas d'estimation possible!", 400);
    }
}