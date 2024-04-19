using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;

namespace skills_sellers.Helpers;

public static class Randomizer
{
    private static readonly string[] AllFoods;
    private static readonly string[] Gutenberg;
    private static readonly string[] AllMuscles;
    private static readonly string[] AllQuotes;
    private static readonly string[] AllDeathQuotes;
    private static readonly object SyncLock = new (); 
    private static List<string> AllCardWords { get; set; } = new();
    private static List<string> AllCardDescriptions { get; set; } = new();

    static Randomizer()
    {
        AllFoods = File.ReadAllLines("all_foods.txt");
        Gutenberg = File.ReadAllLines("gutenberg.txt");
        AllMuscles = File.ReadAllLines("all_muscles.txt");
        AllQuotes = File.ReadAllLines("all_citations.txt");
        AllDeathQuotes = File.ReadAllLines("death_citations.txt");
    }

    public static string RandomPlat(int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        
        var randomLine = random.Next(0, AllFoods.Length);
        return AllFoods[randomLine];
    }
    
    public static string RandomMuscle(int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        
        var randomLine = random.Next(0, AllMuscles.Length);
        return AllMuscles[randomLine];
    }

    public static bool RandomPourcentageUp(int pourcentage = 20)
    {
        var res = RandomInt(0, 100);
        var boolRes = res < pourcentage;
        Console.Out.WriteLine($"Random pourcentage up {res}/{pourcentage} = {boolRes}");
        return boolRes;
    }

    public static bool RandomPourcentageSeeded(string seed, int pourcentage = 20)
    {
        var random = new Random(seed.GetHashCode());
        var res = random.Next(0, 100);
        var boolRes = res < pourcentage;
        Console.Out.WriteLine($"Random pourcentage seeded {seed} : {res}/{pourcentage} = {boolRes}");
        return boolRes;
    }

    public static string RandomCardType(int? seed = null)
    {
        var finalSeed = seed ?? new Random().Next();
        var randomInt = RandomInt(0, 100, finalSeed);
        var type = randomInt switch
        {
            < 1 => "meethic",
            < 4 => "legendaire",
            < 14 => "epic",
            _ => "commune"
        };
        if (seed == null) Console.Out.WriteLine($"Random card res : {randomInt} => {type}");
        
        return type;
    }
    
    public static WeaponAffinity RandomWeaponAffinity(int? seed = null)
    {
        seed ??= new Random().Next();
        return RandomInt(0, 3, seed) switch
        {
            0 => WeaponAffinity.Pierre,
            1 => WeaponAffinity.Ciseaux,
            _ => WeaponAffinity.Feuille
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
            
        // get only wordle characters
        var wordleCharacters = new List<char>
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '.', '-', '+', ',', '(', ')', '[', ']', '|', '!', '?', ':', ';', ' ',
            'é', 'è', 'ê', 'à', 'â', 'î', 'ï', 'ô', 'ù', 'û', 'ç', 'œ', 'æ'
        };
        
        AllCardWords = AllCardWords
            .Select(w => w.ToLower())
            .Where(w => w.All(c => wordleCharacters.Contains(c)))
            .ToList();

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
            
            // remove all '(', ')', '[', ']', '|', '!', '?', ':', ';', ' '
            word = word.Replace("(", "");
            word = word.Replace(")", "");
            word = word.Replace("[", "");
            word = word.Replace("]", "");
            word = word.Replace("|", "");
            word = word.Replace("!", "");
            word = word.Replace("?", "");
            word = word.Replace(":", "");
            word = word.Replace(";", "");
            word = word.Replace(" ", "");
            
            AllCardWords[i] = word;
        }
            
        // remove all words with less than 3 letters
        AllCardWords = AllCardWords.Where(w => w.Length > 2).ToList();
            
        
        return AllCardWords;
    }

    public static List<string> GetAllCardDescriptions(this DbSet<Card> cardsDb)
    {
        if (AllCardDescriptions.Count != 0) return AllCardDescriptions;
        
        AllCardDescriptions = cardsDb
            .Select(c => c.Description)
            .ToList();

        return AllCardDescriptions;
    }
    
    public static string GetRandomCardDescription(this DbSet<Card> cardsDb, string? seed = null)
    {
        var allDesc = GetAllCardDescriptions(cardsDb);
        var random = seed != null ? new Random(seed.GetHashCode()) : new Random();
        var randomLine = random.Next(0, allDesc.Count);
        return allDesc[randomLine];
    }
    
    
    /// <summary>
    /// meethic = 20 pts
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
        // 20 pts pour meethic
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
            "meethic" => 15,
            "legendaire" => 15,
            "epic" => 10,
            "commune" => 5,
            _ => ptsLeft
        };

        while (ptsLeft > 0)
        {
            var index = RandomInt(0, 4);
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

        var explo = GetRandomExploRarityNumber();

        if (explo == 4) Console.Out.WriteLine("4 explo !");
        
        return new Competences
        {
            Force = force,
            Intelligence = intel,
            Cuisine = cuisine,
            Charisme = charisme,
            Exploration = explo
        };
    }

    public static int GetRandomExploRarityNumber()
    {
        var explo = RandomInt(0, 100) switch
        {
            < 40 => 0,
            < 65 => 1,
            < 90 => 2,
            < 99 => 3,
            _ => 4
        };
        return explo;
    }
    
    public static int RandomInt(int min, int max, int? seed = null)
    {
        lock (SyncLock)
        {
            seed ??= new Random().Next();
            return new Random(seed.Value).Next(min, max);
        }
    }
    
    public static Random Random(int? seed = null)
    {
        return seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public static string RandomPlanet()
    {
        var randomLine = RandomInt(0, Gutenberg.Length);
        return Gutenberg[randomLine];
    }

    public static double RandomDouble(int i, int i1)
    {
        lock (SyncLock)
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            var d = BitConverter.ToDouble(bytes, 0);
            var res = i + d * (i1 - i);
            Console.Out.WriteLine($"Random pourcentage up {res} ({i} - {i1})");
            return res;
        }
    }
    
    public static string RandomQuote(string seed)
    {
        var random = new Random(seed.GetHashCode());
        var randomLine = random.Next(0, AllQuotes.Length);
        return AllQuotes[randomLine];
    }
    
    public static string RandomDeathQuote(string seed)
    {
        var random = new Random(seed.GetHashCode());
        var randomLine = random.Next(0, AllDeathQuotes.Length);
        return AllDeathQuotes[randomLine];
    }
}
