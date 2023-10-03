namespace skills_sellers.Helpers;

public static class FoodRandomizer
{
    private static readonly Random Random = new();
    private static readonly string[] AllFoods;

    static FoodRandomizer()
    {
        AllFoods = File.ReadAllLines("all_foods.txt");
    }

    public static string RandomPlat()
    {
        var randomLine = Random.Next(0, AllFoods.Length);
        return AllFoods[randomLine];
    }

    public static bool RandomCuisineUp(int pourcentage = 20)
        => Random.Next(0, 100) < pourcentage;
}
