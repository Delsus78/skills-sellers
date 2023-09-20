using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface ICardService
{
    IEnumerable<Card> GetAll();
    Card GetById(int id);
    void Create(CreateRequest model);
}

public class CardService : ICardService
{
    private readonly DataContext _context;

    public CardService(
        DataContext context)
    {
        _context = context;
    }
    
    public IEnumerable<Card> GetAll()
    {
        return _context.Cards;
    }

    public Card GetById(int id)
    {
        return getCard(id);
    }

    public void Create(CreateRequest model)
    {
        // validate
        if (_context.Cards.Any(x => x.Name == model.Name))
            throw new AppException("Card with the name '" + model.Name + "' already exists", 400);

        // map model to new user object
        var card = model.CreateCard();

        // save user
        _context.Cards.Add(card);
        _context.SaveChanges();
    }
    
    
    // helper methods

    private Card getCard(int id)
    {
        var card = _context.Cards.Find(id);
        if (card == null) throw new KeyNotFoundException("Card not found");
        return card;
    }
}