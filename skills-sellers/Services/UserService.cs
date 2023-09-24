using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Extensions;
using skills_sellers.Models.Users;
using CreateRequest = skills_sellers.Models.Users.CreateRequest;
using UpdateRequest = skills_sellers.Models.Users.UpdateRequest;

namespace skills_sellers.Services;

public interface IUserService
{
    IEnumerable<UserResponse> GetAll();
    UserResponse GetById(int id);
    Task Create(CreateRequest model);
    void Delete(int id);
    void AddCardToUser(int id, int cardId, CompetencesRequest competences);
    Task<AuthenticateResponse> Authenticate(AuthenticateRequest model);
    User GetUserEntity(Expression<Func<User, bool>> predicate);
    IEnumerable<UserCardResponse> GetUserCards(int id);
}

public class UserService : IUserService
{
    private readonly DataContext _context;
    private readonly ICardService _cardService;
    private readonly IAuthService _authService;
    
    public UserService(
        DataContext context,
        ICardService cardService,
        IAuthService authService)
    {
        _context = context;
        _cardService = cardService;
        _authService = authService;
    }

    public IEnumerable<UserResponse> GetAll()
    {
        var users = _context.Users;
        return users.Select(x => x.ToResponse());
    }

    public UserResponse GetById(int id) => GetUserEntity(user => user.Id == id).ToResponse();

    public async Task Create(CreateRequest model)
    {
        // validate
        if (_context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // map model to new user object
        var user = model.CreateUser();

        // registering credentials
        var resultAuthRegister = await _authService.Registeration(user, model.Password, model.Role);
        if (resultAuthRegister.Item1 == 0)
            throw new AppException(resultAuthRegister.Item2, 400);

        // save user
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public void Update(int id, UpdateRequest model)
    {
        var user = GetUserEntity(u => u.Id == id);

        // validate
        if (model.Pseudo != user.Pseudo && _context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // copy model to user and save
        model.UpdateUser(user);
        
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    public void AddCardToUser(int id, int cardId, CompetencesRequest competences)
    {
        var user = GetUserEntity(u => u.Id == id);
        var card = _cardService.GetCardEntity(c => c.Id == cardId);
        
        // Créez une nouvelle instance de UserCard
        var userCard = new UserCard
        {
            User = user,
            Card = card,
            Competences = competences.CreateCompetences()
        };
        
        // Ajoutez cette nouvelle instance à la base de données
        _context.UserCards.Add(userCard);

        // Enregistrez les modifications
        _context.SaveChanges();
    }

    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
        var user = GetUserEntity(u => u.Pseudo == model.Pseudo);

        var loginResult = await _authService.Login(user, model);
        
        if (loginResult.Item1 == 0)
            throw new AppException(loginResult.Item2, 400);
        
        return new AuthenticateResponse(user.Id, user.Pseudo, loginResult.Item2);
    }

    public IEnumerable<UserCardResponse> GetUserCards(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        return user.UserCards.Select(uc => uc.ToUserCardResponse());
    }
    
    // helper methods

    public User GetUserEntity(Expression<Func<User, bool>> predicate)
    {
        var user = IncludeGetUsers().FirstOrDefault(predicate);
        
        if (user == null) throw new AppException("User not found", 404);
        return user;
    }

    private IIncludableQueryable<User,Competences> IncludeGetUsers()
    {
        return _context.Users.Include(u => u.UserCards)
            .ThenInclude(uc => uc.Card)
            .ThenInclude(c => c.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
}