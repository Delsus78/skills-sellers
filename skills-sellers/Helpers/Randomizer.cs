using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;

namespace skills_sellers.Helpers;

public static class Randomizer
{
    private static readonly Random Random = new();
    private static readonly string[] AllFoods;
    private static readonly string[] Gutenberg;
    private static readonly string[] AllMuscles;
    private static List<string> AllCardWords { get; set; } = new();

    static Randomizer()
    {
        AllFoods = File.ReadAllLines("all_foods.txt");
        Gutenberg = File.ReadAllLines("gutenberg.txt");
        AllMuscles = File.ReadAllLines("all_muscles.txt");
    }

    public static string RandomPlat(int? seed = null)
    {
        var random = Random;
        if (seed.HasValue) random = new Random(seed.Value);
        
        var randomLine = random.Next(0, AllFoods.Length);
        return AllFoods[randomLine];
    }
    
    public static string RandomMuscle(int? seed = null)
    {
        var random = Random;
        if (seed.HasValue) random = new Random(seed.Value);
        
        var randomLine = random.Next(0, AllMuscles.Length);
        return AllMuscles[randomLine];
    }

    public static bool RandomPourcentageUp(int pourcentage = 20)
        => Random.Next(0, 100) < pourcentage;
    
    public static string RandomCardType()
    {
        var randomInt = Random.Next(0, 100);
        return randomInt switch
        {
            < 3 => "legendaire",
            < 13 => "epic",
            _ => "commune"
        };
    }

    public static List<string> GetAllCardNameWord(this DbSet<Card> cardsDb)
    {
        if (AllCardWords.Count != 0) return AllCardWords;

        // take all cards name, split them by space, and take a random word
        AllCardWords = cardsDb
            .Select(c => c.Name)
            .ToList()
            .SelectMany(name => name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();
            
        // remove all duplicates
        AllCardWords = AllCardWords.Distinct().ToList();
            
        // remove all words with a '
        AllCardWords = AllCardWords.Where(w => !w.Contains('\'')).ToList();

        // remove ( ) from words
        AllCardWords = AllCardWords.Select(w => w.Replace("(", "").Replace(")", "")).ToList();
        
        // remove ? + ! from words
        AllCardWords = AllCardWords.Select(w => w.Replace("?", "").Replace("!", "")).ToList();
        
        
        // remove all accents and replace them by the letter without accent
        for (var i = 0; i < AllCardWords.Count; i++)
        {
            var word = AllCardWords[i];
            word = word.Replace("é", "e");
            word = word.Replace("è", "e");
            word = word.Replace("ê", "e");
            word = word.Replace("à", "a");
            word = word.Replace("â", "a");
            word = word.Replace("î", "i");
            word = word.Replace("ï", "i");
            word = word.Replace("ô", "o");
            word = word.Replace("ù", "u");
            word = word.Replace("û", "u");
            word = word.Replace("ç", "c");
            word = word.Replace("œ", "oe");
            word = word.Replace("æ", "ae");
            AllCardWords[i] = word;
        }
            
        // remove all words with less than 3 letters
        AllCardWords = AllCardWords.Where(w => w.Length > 2).ToList();
            
        
        return AllCardWords;
    }

    /// <summary>
    /// legendaire = 15 pts
    /// epic = 10 pts
    /// commune = 5 pts
    ///
    /// Les pts d’EXPLO sont calculé via un autre taux : 
    /// 40% d’avoir 0, 
    /// 25% d’avoir 1
    /// 25% d’avoir 2
    /// 9% d’avoir 3
    /// 1% d’avoir 4
    /// </summary>
    /// <param name="rarity"></param>
    /// <returns></returns>
    public static Competences GetRandomCompetenceBasedOnRarity(string rarity)
    {
        // repartir les points en fonction de la rareté sur les 4 stats (force, intel, cuisine, charisme)
        // chaque compétence ne peut pas dépasser 10 pts
        // 15 pts pour legendaire
        // 10 pts pour epic
        // 5 pts pour commune
        var ptsLeft = 0;
        var valuesDicto = new Dictionary<string, int>
        {
            { "force", 0 },
            { "intel", 0 },
            { "cuisine", 0 },
            { "charisme", 0 }
        };
        var stillAvailable = new List<string> { "force", "intel", "cuisine", "charisme" };

        ptsLeft = rarity switch
        {
            "legendaire" => 15,
            "epic" => 10,
            "commune" => 5,
            _ => ptsLeft
        };

        while (ptsLeft > 0)
        {
            var index = Random.Next(0, 4);
            var key = stillAvailable[index];
            if (valuesDicto[key] < 10)
            {
                valuesDicto[key] += 1;
                ptsLeft -= 1;
            }
            else stillAvailable.RemoveAt(index);

        }
        
        var force = valuesDicto["force"];
        var intel = valuesDicto["intel"];
        var cuisine = valuesDicto["cuisine"];
        var charisme = valuesDicto["charisme"];

        var explo = Random.Next(0, 100) switch
        {
            < 40 => 0,
            < 65 => 1,
            < 90 => 2,
            < 99 => 3,
            _ => 4
        };

        return new Competences
        {
            Force = force,
            Intelligence = intel,
            Cuisine = cuisine,
            Charisme = charisme,
            Exploration = explo
        };
    }
    
    
    public static int RandomInt(int min, int max)
        => Random.Next(min, max);

    public static string RandomPlanet()
    {
        var randomLine = Random.Next(0, Gutenberg.Length);
        return Gutenberg[randomLine];
    }
}
