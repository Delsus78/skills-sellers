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
    ActionEstimationResponse EstimateAction(User user, ActionRequest model);
    Task<UserBatimentResponse> SetLevelOfBatiments(int id, UserBatimentRequest batimentsRequest);
    Task<UserCardResponse?> OpenCard(User user);
    Task<UserCardResponse?> OpenCard(int userId);
    Task<UserCardResponse> AmeliorerCard(User user, int userCardId, CompetencesRequest competencesRequest);
    UserCardResponse GetUserCard(int id, int cardId);
    Task<IEnumerable<NotificationResponse>> GetNotifications(User user);
    Task DeleteNotification(User user, int notificationId);
    Task SendNotificationToAll(NotificationRequest notification);
    RegistrationLinkResponse CreateLink(LinkCreateRequest model);
    Task<UserResponse> Register(UserRegisterRequest model);
}

public class UserService : IUserService
{
    private readonly DataContext _context;
    private readonly ICardService _cardService;
    private readonly IAuthService _authService;
    private readonly IStatsService _statsService;
    private readonly IUserBatimentsService _userBatimentsService;
    private readonly INotificationService _notificationService;
    private readonly IRegistrationLinkCreatorService _registrationLinkCreatorService;
    
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
        IActionService<ActionAmeliorer> ameliorerActionService, 
        INotificationService notificationService,
        IRegistrationLinkCreatorService registrationLinkCreatorService)
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
        _notificationService = notificationService;
        _registrationLinkCreatorService = registrationLinkCreatorService;
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        user = GetUserEntity(u => u.Pseudo == model.Pseudo);
        
        // create user stats
        _statsService.GetOrCreateStatsEntity(user);
        
        // create user batiment data
        _userBatimentsService.GetOrCreateUserBatimentData(user);

        // registering credentials
        var resultAuthRegister = await _authService.Registeration(user, model.Password, model.Role);
        if (resultAuthRegister.Item1 == 0)
            throw new AppException(resultAuthRegister.Item2, 400);

        // create first card
        AddCardToUser(user.Id, model.FirstCardId, new CompetencesRequest(3, 3, 3, 3, 3));

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
    
    public UserCardResponse GetUserCard(int id, int cardId)
    {
        var user = GetUserEntity(u => u.Id == id);
        var userCard = user.UserCards.FirstOrDefault(uc => uc.CardId == cardId);
        if (userCard == null)
            throw new AppException("Le joueur ne possède pas cette carte !", 404);

        return userCard.ToResponse();
    }

    public async Task<IEnumerable<NotificationResponse>> GetNotifications(User user)
        => await _notificationService.GetNotifications(user);

    public Task DeleteNotification(User user, int notificationId)
        => _notificationService.DeleteNotification(user, notificationId);

    public async Task SendNotificationToAll(NotificationRequest notification)
    {
        await _notificationService.SendNotificationToAll(notification, _context);
        await _context.SaveChangesAsync();
    }

    public RegistrationLinkResponse CreateLink(LinkCreateRequest model)
    {
        return new RegistrationLinkResponse(_registrationLinkCreatorService.CreateRegistrationLink(model.Role, model.FirstCardId));
    }

    public async Task<UserResponse> Register(UserRegisterRequest model)
    {
        // link verification
        var linkInfo = _registrationLinkCreatorService.GetLink(model.link);
        
        if (!linkInfo.valid)
            throw new AppException("Link not valid", 400);
        
        // user creation
        var user = await Create(new UserCreateRequest(model.Pseudo, model.Password, linkInfo.role, linkInfo.firstCardId));

        // link deletion
        _registrationLinkCreatorService.DeleteLink(model.link);
        
        return user;
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

        var nbLaboUsed = _ameliorerActionService.GetActions().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        var nbSalleMuscuUsed = _musclerActionService.GetActions().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        var nbSalleExplorerUsed = _explorerActionService.GetActions().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        
        return userBatimentData.ToResponse(nbSalleMuscuUsed, nbLaboUsed, nbSalleExplorerUsed);
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
    
    public ActionEstimationResponse EstimateAction(User user, ActionRequest model)
    {
        return model.ActionName switch
        {
            "cuisiner" => _cuisinerActionService.EstimateAction(user, model),
            "explorer" => _explorerActionService.EstimateAction(user, model),
            "muscler" => _musclerActionService.EstimateAction(user, model),
            "ameliorer" => _ameliorerActionService.EstimateAction(user, model),
            _ => throw new AppException("Action not found", 404)
        };
    }

    public async Task<UserBatimentResponse> SetLevelOfBatiments(int id, UserBatimentRequest batimentsRequest)
    {
        var user = GetUserEntity(u => u.Id == id);
        
        var userBatimentData = batimentsRequest.UpdateUserBatimentData(user.UserBatimentData);
        
        // save user batiment data
        _context.UserBatiments.Update(userBatimentData);
        await _context.SaveChangesAsync();

        return userBatimentData.ToResponse(-1, -1, -1);
    }

    public async Task<UserCardResponse?> OpenCard(User user)
    {
        // remove card opening
        user.NbCardOpeningAvailable--;
        
        // random card
        var card = _cardService.GetRandomCard();
        
        // Doublon
        // check if user has already this card
        var userCards = user.UserCards;
        var doublon = userCards.FirstOrDefault(uc => uc.CardId == card.Id);
        
        if (doublon != null)
        {
            var usercardDoubled = new UserCardDoubled
            {
                User = user,
                Card = card
            };
            user.UserCardsDoubled.Add(usercardDoubled);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return null;
        }

        // random competences
        var competence = Randomizer.GetRandomCompetenceBasedOnRarity(card.Rarity);
        
        var userCardEntity = new UserCard
        {
            User = user,
            Card = card,
            Competences = competence,
            Action = null
        };

        // save user card
        _context.UserCards.Add(userCardEntity);
        
        // save user
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return userCardEntity.ToResponse();
    }

    public Task<UserCardResponse?> OpenCard(int userId)
    {
        var user = GetUserEntity(u => u.Id == userId);
        return OpenCard(user);
    }

    public async Task<UserCardResponse> AmeliorerCard(User user, int userCardId, CompetencesRequest competencesRequest)
    {
        // get user card
        var userCard = user.UserCards.FirstOrDefault(uc => uc.CardId == userCardId);

        if (userCard == null)
            throw new AppException("User card not found", 404);

        var doubledEntity = user.UserCardsDoubled.FirstOrDefault(ucd => ucd.CardId == userCard.CardId);
        
        if (doubledEntity == null)
            throw new AppException("User card doubled not found => unauthorized", 404);
        
        // verifier si pour la carte en question, la rareté correspond au nombre de points de compétence envoyé
        var nbPoints = competencesRequest.Force +
                       competencesRequest.Intelligence +
                       competencesRequest.Cuisine +
                       competencesRequest.Charisme +
                       competencesRequest.Exploration;

        var tooMuchPts = userCard.Card.Rarity.ToLower() switch
        {
            "legendaire" => nbPoints > 15,
            "epic" => nbPoints > 10,
            "commune" => nbPoints > 5,
            _ => throw new AppException("Rarity not found", 404)
        };
        
        if (tooMuchPts)
            throw new AppException("Too much points for this rarity", 400);

        // update user card
        userCard.Competences.Intelligence += competencesRequest.Intelligence + userCard.Competences.Intelligence > 10 ? 0 : competencesRequest.Intelligence;
        userCard.Competences.Force += competencesRequest.Force + userCard.Competences.Force > 10 ? 0 : competencesRequest.Force;
        userCard.Competences.Cuisine += competencesRequest.Cuisine + userCard.Competences.Cuisine > 10 ? 0 : competencesRequest.Cuisine;
        userCard.Competences.Charisme += competencesRequest.Charisme + userCard.Competences.Charisme > 10 ? 0 : competencesRequest.Charisme;
        userCard.Competences.Exploration += competencesRequest.Exploration + userCard.Competences.Exploration > 10 ? 0 : competencesRequest.Exploration;
        
        _context.UserCards.Update(userCard);
        
        // save user
        user.UserCardsDoubled.Remove(doubledEntity);
        _context.Users.Update(user);
        
        // save changes
        await _context.SaveChangesAsync();
        
        return userCard.ToResponse();
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
            .ThenInclude(uc => uc.Competences)
            .Include(u => u.UserBatimentData)
            .Include(u => u.UserCardsDoubled);
    }
}