using skills_sellers.Entities;

namespace skills_sellers.Helpers;

public static class Randomizer
{
    private static readonly Random Random = new();
    private static readonly string[] AllFoods;
    private static readonly string[] Gutenberg;

    static Randomizer()
    {
        AllFoods = File.ReadAllLines("all_foods.txt");
        Gutenberg = File.ReadAllLines("gutenberg.txt");
    }

    public static string RandomPlat()
    {
        var randomLine = Random.Next(0, AllFoods.Length);
        return AllFoods[randomLine];
    }

    public static bool RandomPourcentageUp(int pourcentage = 20)
        => Random.Next(0, 100) < pourcentage;
    
    public static string RandomCardType()
    {
        var randomInt = Random.Next(0, 100);
        return randomInt switch
        {
            < 5 => "legendaire",
            < 25 => "epic",
            _ => "commune"
        };
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
        // 15 pts pour legendaire
        // 10 pts pour epic
        // 5 pts pour commune
        var force = 0;
        var intel = 0;
        var cuisine = 0;
        var charisme = 0;
        switch (rarity.ToLower())
        {
            case "legendaire":
                force = Random.Next(0, 15);
                intel = Random.Next(0, 15 - force);
                cuisine = Random.Next(0, 15 - force - intel);
                charisme = 15 - force - intel - cuisine;
                break;
            case "epic":
                force = Random.Next(0, 10);
                intel = Random.Next(0, 10 - force);
                cuisine = Random.Next(0, 10 - force - intel);
                charisme = 10 - force - intel - cuisine;
                break;
            case "commune":
                force = Random.Next(0, 5);
                intel = Random.Next(0, 5 - force);
                cuisine = Random.Next(0, 5 - force - intel);
                charisme = 5 - force - intel - cuisine;
                break;
        }
        
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
