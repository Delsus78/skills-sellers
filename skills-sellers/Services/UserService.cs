using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using skills_sellers.Models.Users;

namespace skills_sellers.Services;

public interface IUserService
{
    IEnumerable<User> GetAll();
    User GetById(int id);
    Task Create(CreateRequest model);
    void Update(int id, UpdateRequest model);
    void Delete(int id);
    
    void AddCardToUser(int id, int cardId);
    Task<AuthenticateResponse> Authenticate(AuthenticateRequest model);
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

    public IEnumerable<User> GetAll()
    {
        return _context.Users;
    }

    public User GetById(int id)
    {
        return GetUser(id);
    }

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
        var user = GetUser(id);

        // validate
        if (model.Pseudo != user.Pseudo && _context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // hash password if it was entered
        //if (!string.IsNullOrEmpty(model.Password))
        //    user.PasswordHash = BCrypt.HashPassword(model.Password);

        // copy model to user and save
        model.UpdateUser(user);
        
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var user = GetUser(id);
        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    public void AddCardToUser(int id, int cardId)
    {
        var user = GetUser(id);
        var card = _cardService.GetById(cardId);
        user.Cards.Add(card);
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
        var user = GetUser(model.Pseudo);

        var loginResult = await _authService.Login(user, model);
        
        if (loginResult.Item1 == 0)
            throw new AppException(loginResult.Item2, 400);
        
        return new AuthenticateResponse(user.Id, user.Pseudo, loginResult.Item2);
    }

    // helper methods

    private User GetUser(int id)
    {
        var user = _context.Users.Include(u => u.Cards)
            .FirstOrDefault(u => u.Id == id);
        if (user == null) throw new AppException("User not found", 404);
        return user;
    }

    private User GetUser(string pseudo)
    {
        var user = _context.Users.Include(u => u.Cards)
            .FirstOrDefault(u => u.Pseudo == pseudo);
        if (user == null) throw new AppException("User not found", 404);
        return user;
    }
}