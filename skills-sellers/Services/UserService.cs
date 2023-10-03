using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Cards;
using skills_sellers.Models.Extensions;
using skills_sellers.Models.Users;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services;

public interface IUserService
{
    IEnumerable<UserResponse> GetAll();
    UserResponse GetById(int id);
    Task<UserResponse> Create(UserCreateRequest model);
    void Delete(int id);
    void AddCardToUser(int id, int cardId, CompetencesRequest competences);
    Task<AuthenticateResponse> Authenticate(AuthenticateRequest model);
    User GetUserEntity(Expression<Func<User, bool>> predicate);
    IEnumerable<UserCardResponse> GetUserCards(int id);
    StatsResponse GetUserStats(int id);
    UserBatimentResponse GetUserBatiments(int id);
    Task<ActionResponse> CreateAction(User user, ActionRequest model);
}

public class UserService : IUserService
{
    private readonly DataContext _context;
    private readonly ICardService _cardService;
    private readonly IAuthService _authService;
    private readonly IStatsService _statsService;
    private readonly IUserBatimentsService _userBatimentsService;
    
    // actions services
    private readonly IActionService<ActionCuisiner> _cuisinerActionService;
    private readonly IActionService<ActionExplorer> _explorerActionService;
    private readonly IActionService<ActionMuscler> _musclerActionService;
    private readonly IActionService<ActionAmeliorer> _ameliorerActionService;

    public UserService(
        DataContext context,
        ICardService cardService,
        IAuthService authService,
        IStatsService statsService, 
        IUserBatimentsService userBatimentsService,
        IActionService<ActionCuisiner> cuisinerActionService,
        IActionService<ActionExplorer> explorerActionService,
        IActionService<ActionMuscler> musclerActionService,
        IActionService<ActionAmeliorer> ameliorerActionService)
    {
        _context = context;
        _cardService = cardService;
        _authService = authService;
        _statsService = statsService;
        _userBatimentsService = userBatimentsService;
        _cuisinerActionService = cuisinerActionService;
        _explorerActionService = explorerActionService;
        _musclerActionService = musclerActionService;
        _ameliorerActionService = ameliorerActionService;
    }

    public IEnumerable<UserResponse> GetAll()
    {
        var users = IncludeGetUsers().ToList();
        return users.Select(x => x.ToResponse());
    }

    public UserResponse GetById(int id) => GetUserEntity(user => user.Id == id).ToResponse();

    public async Task<UserResponse> Create(UserCreateRequest model)
    {
        // validate
        if (_context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // map model to new user object
        var user = model.CreateUser();
        
        // create user stats
        _statsService.GetOrCreateStatsEntity(user);
        
        // create user batiment data
        _userBatimentsService.GetOrCreateUserBatimentData(user);

        // registering credentials
        var resultAuthRegister = await _authService.Registeration(user, model.Password, model.Role);
        if (resultAuthRegister.Item1 == 0)
            throw new AppException(resultAuthRegister.Item2, 400);

        // save user
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user.ToResponse();
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
        return user.UserCards.Select(uc => uc.ToResponse());
    }

    public StatsResponse GetUserStats(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        var userCards = user.UserCards;
        var stats = _statsService.GetOrCreateStatsEntity(user);
        
        return stats.ToResponse(userCards);
    }

    public UserBatimentResponse GetUserBatiments(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        var userBatimentData = _userBatimentsService.GetOrCreateUserBatimentData(user);
        return userBatimentData.ToResponse();
    }

    public async Task<ActionResponse> CreateAction(User user, ActionRequest model)
    {
        return model.ActionName switch
        {
            "cuisiner" => await _cuisinerActionService.StartAction(user, model),
            "explorer" => await _explorerActionService.StartAction(user, model),
            "muscler" => await _musclerActionService.StartAction(user, model),
            "ameliorer" => await _ameliorerActionService.StartAction(user, model),
            _ => throw new AppException("Action not found", 404)
        };
    }

    // helper methods

    public User GetUserEntity(Expression<Func<User, bool>> predicate)
    {
        var user = IncludeGetUsers().FirstOrDefault(predicate);
        
        if (user == null) throw new AppException("User not found", 404);
        return user;
    }
    
    

    private IIncludableQueryable<User,Object> IncludeGetUsers()
    {
        // include usercards of user, cards of usercards, competences of usercards and actions of usercards
        return _context.Users
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Action)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Card)
            .ThenInclude(c => c.UserCards)
            .ThenInclude(uc => uc.Competences);
    } 
}