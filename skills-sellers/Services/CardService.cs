using System.Linq.Expressions;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface ICardService
{
    IEnumerable<CardResponse> GetAll();
    CardResponse GetById(int id);
    CardResponse Create(CardCreateRequest model);
    Card GetCardEntity(Expression<Func<Card, bool>> predicate);
}

public class CardService : ICardService
{
    private readonly DataContext _context;

    public CardService(
        DataContext context)
    {
        _context = context;
    }
    
    public IEnumerable<CardResponse> GetAll() => _context.Cards.Select(x => x.ToResponse());

    public CardResponse GetById(int id) => GetCardEntity(c => c.Id == id).ToResponse();

    public CardResponse Create(CardCreateRequest model)
    {
        // validate
        if (_context.Cards.Any(x => x.Name == model.Name))
            throw new AppException("Card with the name '" + model.Name + "' already exists", 400);

        // map model to new user object
        var card = model.CreateCard();

        // save user
        _context.Cards.Add(card);
        _context.SaveChanges();
        
        return card.ToResponse();
    }
    
    
    // helper methods

    public Card GetCardEntity(Expression<Func<Card, bool>> predicate)
    {
        var card = _context.Cards.FirstOrDefault(predicate);
        
        if (card == null) throw new KeyNotFoundException("Card not found");
        return card;
    }
}