namespace skills_sellers.Helpers;

public static class FoodRandomizer
{
    private static readonly Random Random = new Random();
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
}
