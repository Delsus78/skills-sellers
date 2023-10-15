using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services;

public interface IMarchandService
{
    Task BuyMarchandAsync(User user);
    MarchandShopResponse GetMarchandShop();
}
public class MarchandService : IMarchandService
{
    private readonly DataContext _context;
    private readonly IUserBatimentsService _userBatimentsService;

    public const int MaxBuyMarchandPerDay = 1;

    public MarchandService(
        DataContext context, 
        IUserBatimentsService userBatimentsService)
    {
        _context = context;
        _userBatimentsService = userBatimentsService;
    }

    private int GetRandomSeededTodayFood(int seed)
        => new Random(seed).Next(2, 7);


    public async Task BuyMarchandAsync(User user)
    {
        var userBatiment = _userBatimentsService.GetOrCreateUserBatimentData(user);

        if (userBatiment.NbBuyMarchandToday >= MaxBuyMarchandPerDay)
            throw new AppException($"Vous ne pouvez acheter que {MaxBuyMarchandPerDay} fois par jour !", 400);

        var foodAmount = GetRandomSeededTodayFood(DateTime.Today.DayOfYear);
        var price = foodAmount * 5;
        
        if (user.Or < price)
            throw new AppException("Vous n'avez pas assez d'or !", 400);

        user.Or -= price;
        user.Nourriture += foodAmount;
        userBatiment.NbBuyMarchandToday++;

        await _context.SaveChangesAsync();
    }

    public MarchandShopResponse GetMarchandShop()
    {
        var seed = DateTime.Today.DayOfYear;
        var foodAmount = GetRandomSeededTodayFood(seed);
        var foodName = Randomizer.RandomPlat(seed);
        var price = foodAmount * 5;
        
        return new MarchandShopResponse(foodAmount, price, foodName);
    }
}