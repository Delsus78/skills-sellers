using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Registres;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
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
    Task<List<ActionResponse>> CreateAction(User user, ActionRequest model);
    ActionEstimationResponse EstimateAction(User user, ActionRequest model);
    Task<UserBatimentResponse> SetLevelOfBatiments(int id, UserBatimentRequest batimentsRequest);
    Task<UserCardResponse?> OpenCard(User user);
    Task<UserCardResponse?> OpenCard(int userId);
    Task<UserCardResponse> AmeliorerCard(User user, int userCardId, CompetencesRequest competencesRequest);
    Task<UserWeaponResponse> AmeliorerWeapon(User user, int weaponId, bool fromUpgradePoint = true);
    UserCardResponse GetUserCard(User user, int cardId);
    Task<IEnumerable<NotificationResponse>> GetNotifications(User user);
    Task DeleteNotifications(User user, List<int> notificationIds);
    Task SendNotificationToAll(NotificationRequest notification);
    Task SendNotification(User user, int userId, NotificationRequest notificationRequest);
    RegistrationLinkResponse CreateLink(LinkCreateRequest model);
    Task<UserResponse> Register(UserRegisterRequest model);
    Task<ResetPasswordLinkResponse> CreateResetPasswordLink(ResetPasswordLinkRequest model);
    Task<AuthenticateResponse> ResetPassword(ResetPasswordRequest model);
    Task DeleteAction(User user, int actionId);
    Task<GiftCodeResponse> EnterGiftCode(User user, GiftCodeRequest giftCode);
    Task<GiftCodeResponse> CreateGiftCode(GiftCodeCreationRequest giftCodeCreationRequest);
    Task ResponseToBottedAgent(User user);
    IEnumerable<UserWeaponResponse> GetUserWeapons(int id);
    UserWeaponResponse GetUserWeapon(int id, int weaponId);
    Task<ActionResponse> DecideForAction(User user, ActionDecisionRequest model);
    UserRegistreInfoResponse GetRegistreInfo(int id);
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
    private readonly IActionTaskService _actionTaskService;

    public UserService(
        DataContext context,
        ICardService cardService,
        IAuthService authService,
        IStatsService statsService, 
        IUserBatimentsService userBatimentsService,
        INotificationService notificationService,
        IRegistrationLinkCreatorService registrationLinkCreatorService, IActionTaskService actionTaskService)
    {
        _context = context;
        _cardService = cardService;
        _authService = authService;
        _statsService = statsService;
        _userBatimentsService = userBatimentsService;
        _notificationService = notificationService;
        _registrationLinkCreatorService = registrationLinkCreatorService;
        _actionTaskService = actionTaskService;
    }

    #region USER
    public IEnumerable<UserResponse> GetAll()
    {
        var users = _context.Users
            .SelectUserDetails();
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

    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
        var user = GetUserEntity(u => u.Pseudo == model.Pseudo);

        var loginResult = await _authService.Login(user, model);
        
        if (loginResult.Item1 == 0)
            throw new AppException(loginResult.Item2, 400);
        
        return new AuthenticateResponse(user.Id, user.Pseudo, loginResult.Item2);
    }
    
    public RegistrationLinkResponse CreateLink(LinkCreateRequest model)
    {
        return new RegistrationLinkResponse(_registrationLinkCreatorService.CreateRegistrationLink(model.Role, model.FirstCardId));
    }
    
    public async Task<AuthenticateResponse> ResetPassword(ResetPasswordRequest model)
    {
        // link verification
        var linkInfo = _registrationLinkCreatorService.GetResetPasswordLink(model.link);
        
        if (!linkInfo.valid)
            throw new AppException("Link not valid", 400);
        
        // change password
        var result = await _authService.ResetPassword(linkInfo.userId, model.Password);
        
        if (result.Item1 == 0)
            throw new AppException(result.Item2, 400);
        
        // link deletion
        _registrationLinkCreatorService.DeletePasswordLink(model.link);
        
        // authenticate
        var userPseudo = _context.Users.FirstOrDefault(u => u.Id == linkInfo.userId)?.Pseudo;
        if (userPseudo == null)
            throw new AppException("User not found", 404);
        
        return await Authenticate(new AuthenticateRequest(userPseudo, model.Password));
    }

    public Task<ResetPasswordLinkResponse> CreateResetPasswordLink(ResetPasswordLinkRequest model)
    {
        var userId = _context.Users.FirstOrDefault(u => u.Pseudo == model.Pseudo)?.Id;
        if (!userId.HasValue)
            throw new AppException("User not found", 404);
        
        var link = _registrationLinkCreatorService.CreateResetPasswordLink(userId.Value);
        
        return Task.FromResult(new ResetPasswordLinkResponse(link));
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
    
    #endregion
    
    #region USERCARDS
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

    public IEnumerable<UserCardResponse> GetUserCards(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        return user.UserCards.Select(uc => uc.ToResponse());
    }
    
    public UserCardResponse GetUserCard(User user, int cardId)
    {
        var userCard = user.UserCards.FirstOrDefault(uc => uc.CardId == cardId);
        if (userCard == null)
            throw new AppException("Le joueur ne possède pas cette carte !", 404);

        return userCard.ToResponse();
    }
    
    public async Task<UserCardResponse?> OpenCard(User user)
    {
        // 0 card ?
        if (user.NbCardOpeningAvailable <= 0)
            throw new AppException("Vous n'avez plus d'ouverture de carte disponible !", 400);
        
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
            _statsService.OnDoublonsEarned(user.Id);
            await _context.SaveChangesAsync();
            return null;
        }

        // random competences
        var competence = Randomizer.GetRandomCompetenceBasedOnRarity(card.Rarity);
        
        // special case for legendary
        if (card.Rarity == "legendaire")
            competence.Exploration++;
        
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
        var doubledEntity = user.UserCardsDoubled.MaxBy(ucd => ucd.Id);
        var userCard = user.UserCards.FirstOrDefault(uc => uc.CardId == userCardId);
        
        // check if its the last doublon sorted by doublon id
        if (doubledEntity?.CardId != userCardId)
            throw new AppException("You can't upgrade this card !", 400);

        if (userCard == null)
            throw new AppException("User card not found", 404);

        // verifier si pour la carte en question, la rareté correspond au nombre de points de compétence envoyé
        var nbPoints = competencesRequest.Force +
                       competencesRequest.Intelligence +
                       competencesRequest.Cuisine +
                       competencesRequest.Charisme +
                       competencesRequest.Exploration;

        var maxPointsAccepted = userCard.Card.Rarity.ToLower() switch
        {
            "legendaire" => 3,
            "epic" => 2,
            "commune" => 1,
            _ => throw new AppException("Rarity not found", 404)
        };
        
        if (nbPoints > maxPointsAccepted)
            throw new AppException("Too much points for this rarity", 400);

        // special case : card is already at 10 10 10 10 10 (A CHANGER QUAND LES CARTES POURRONT MONTER A 11)
        if (userCard.Competences is { Intelligence: 10, Force: 10, Cuisine: 10, Charisme: 10, Exploration: 10 })
        {
            // refund
            user.Or += 1000 * maxPointsAccepted;
            
            // notify user
            await _notificationService.SendNotificationToUser(user, new NotificationRequest(
                   "Doublon remboursé !", 
                   $"Votre doublon de {userCard.Card.Name} a été remboursé car vous avez déjà toutes les compétences à 10 ! Vous avez reçu {1000 * maxPointsAccepted} or !"), 
                _context);
        }
        
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
    
    #endregion

    #region USERWEAPONS

    public IEnumerable<UserWeaponResponse> GetUserWeapons(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        return user.UserWeapons.Select(uc => uc.ToResponse());
    }

    public UserWeaponResponse GetUserWeapon(int id, int weaponId)
    {
        var user = GetUserEntity(u => u.Id == id);
        return user.UserWeapons.FirstOrDefault(uc => uc.Id == weaponId)?.ToResponse() ?? throw new AppException("User weapon not found", 404);
    }

    public async Task<UserWeaponResponse> AmeliorerWeapon(User user, int weaponId, bool fromUpgradePoint = true)
    {
        if (fromUpgradePoint && user.NbWeaponUpgradeAvailable <= 0)
            throw new AppException("Vous n'avez plus d'amélioration d'arme disponible !", 400);
        
        // get user weapon
        var userWeapon = user.UserWeapons.FirstOrDefault(uw => uw.Id == weaponId);
        if (userWeapon == null)
            throw new AppException("User weapon not found", 404);
        
        // impossible if equiped or in upgrade
        // weapon in labo ?
        if (user.UserCards.Any(card => card.Action is ActionAmeliorer ameliorer && ameliorer.WeaponToUpgradeId == weaponId))
            throw new AppException("Cette arme est en amélioration", 400);
        
        if (userWeapon.UserCard != null)
            throw new AppException("Cette arme est équipée, déséquipez la avant !", 400);
        
        // up power of weapon
        userWeapon.Power++;
        
        // remove upgrade point
        if (fromUpgradePoint)
            user.NbWeaponUpgradeAvailable--;
        
        // save user weapon
        await _context.SaveChangesAsync();
        
        return userWeapon.ToResponse();
    }

    #endregion
    
    #region Stats and notifications
    public async Task<IEnumerable<NotificationResponse>> GetNotifications(User user)
        => await _notificationService.GetNotifications(user);

    public Task DeleteNotifications(User user, List<int> notificationIds)
        => _notificationService.DeleteNotifications(user, notificationIds);

    public async Task SendNotificationToAll(NotificationRequest notification)
    {
        await _notificationService.SendNotificationToAll(notification, _context);
        await _context.SaveChangesAsync();
    }

    public async Task SendNotification(User user, int userId, NotificationRequest notificationRequest)
    {
        var userToNotify = GetUserEntity(u => u.Id == userId);

        // price of 10 or
        if (user.Or < 10)
            throw new AppException("Vous n'avez pas assez d'or pour envoyer une notification !", 400);
        
        // remove 10 or
        user.Or -= 10;

        await _notificationService.SendNotificationToUser(userToNotify, notificationRequest with { Title = "DM de " + user.Pseudo }, _context);
        await _context.SaveChangesAsync();
    }

    public StatsResponse GetUserStats(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        var userCards = user.UserCards;
        var stats = _statsService.GetOrCreateStatsEntity(user);
        var nbCardsInBdd = _cardService.GetCount();
        
        // get ranks
        var ranks = _statsService.GetRanks(user);

        return stats.ToResponse(userCards, ranks, nbCardsInBdd);
    }

    #endregion

    #region ACTIONS

    public async Task<List<ActionResponse>> CreateAction(User user, ActionRequest model) 
        => await _actionTaskService.CreateNewActionAsync(user.Id, model);

    public ActionEstimationResponse EstimateAction(User user, ActionRequest model) 
        => _actionTaskService.EstimateAction(user.Id, model);

    public async Task DeleteAction(User user, int actionId)
        => await _actionTaskService.DeleteActionAsync(user.Id, actionId);

    
    public Task<ActionResponse> DecideForAction(User user, ActionDecisionRequest model)
    {
        var action = _context.Actions.FirstOrDefault(a => a.Id == model.ActionId);
        if (action == null)
            throw new AppException("Action not found", 404);

        if (action is not ActionExplorer actionExplorer)
            throw new AppException("Action is not an explorer action", 400);
        
        if (action.UserId != user.Id)
            throw new AppException("Action is not yours", 400);

        if (actionExplorer.Decision != null)
            throw new AppException("Action already decided", 400);

        // update action
        actionExplorer.Decision = model.Decision;
        _context.Actions.Update(actionExplorer);

        // save changes
        _context.SaveChanges();
        

        // end action if already finished
        if (!_actionTaskService.IsActionRunning(actionExplorer.Id))
            _actionTaskService.StartNewTaskForAction(actionExplorer);
        
        return Task.FromResult(actionExplorer.ToResponse());
    }

    public UserRegistreInfoResponse GetRegistreInfo(int id)
    {
        var user = GetUserEntity(u => u.Id == id);
        var userRegistreInfo = _context.UserRegistreInfos.FirstOrDefault(ri => ri.UserId == user.Id);
        if (userRegistreInfo == null) // create registre info if not exist
        {
            userRegistreInfo = new UserRegistreInfo
            {
                User = user,
                HostileAttackWon = 0,
                HostileAttackLost = 0
            };
            _context.UserRegistreInfos.Add(userRegistreInfo);
            _context.SaveChanges();
        }
        
        // get registres of user
        _context.Entry(user)
            .Collection(u => u.Registres)
            .Load();
        
        // load relatedPlayers of registrePlayer
        _context.Entry(user)
            .Collection(u => u.Registres)
            .Query()
            .OfType<RegistrePlayer>()
            .Include(rp => rp.RelatedPlayer)
            .Load();
        
        return userRegistreInfo.ToResponse(user.Registres);
    }
    
    #endregion
    
    #region BATIMENTS
    
    public UserBatimentResponse GetUserBatiments(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
            throw new AppException("User not found", 404);
        
        var userBatimentData = _userBatimentsService.GetOrCreateUserBatimentData(user);

        var nbLaboUsed = _context.Actions.OfType<ActionAmeliorer>().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        var nbSalleMuscuUsed = _context.Actions.OfType<ActionMuscler>().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        var nbSalleExplorerUsed = _context.Actions.OfType<ActionExplorer>().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        var nbSatelliteUsed = _context.Actions.OfType<ActionSatellite>().Count(act => act.UserCards.Any(uc => uc.UserId == user.Id));
        
        return userBatimentData.ToResponse(nbSalleMuscuUsed, nbLaboUsed, nbSalleExplorerUsed, nbSatelliteUsed);
    }
    
    public async Task<UserBatimentResponse> SetLevelOfBatiments(int id, UserBatimentRequest batimentsRequest)
    {
        var user = GetUserEntity(u => u.Id == id);
        
        var userBatimentData = batimentsRequest.UpdateUserBatimentData(user.UserBatimentData);
        
        // save user batiment data
        _context.UserBatiments.Update(userBatimentData);
        await _context.SaveChangesAsync();

        return userBatimentData.ToResponse();
    }
    
    #endregion

    #region GIFTS

    public async Task<GiftCodeResponse> CreateGiftCode(GiftCodeCreationRequest giftCodeCreationRequest)
    {
        var code = Guid.NewGuid().ToString()[..8];
        var giftCode = giftCodeCreationRequest.CreateGiftCode(code);
        
        _context.GiftCodes.Add(giftCode);
        await _context.SaveChangesAsync();

        return giftCode.ToResponse();
    }
    
    public async Task<GiftCodeResponse> EnterGiftCode(User user, GiftCodeRequest giftCode)
    {
        var giftCodeEntity = _context.GiftCodes.FirstOrDefault(gc => gc.Code == giftCode.Code);
        if (giftCodeEntity == null)
            throw new AppException("Ce code cadeau n'existe pas !", 404);

        if (giftCodeEntity.Used)
            throw new AppException("Ce code cadeau a déjà été utilisé !", 400);

        // add resources
        user.NbCardOpeningAvailable += giftCodeEntity.NbCards;
        user.Creatium += giftCodeEntity.NbCreatium;
        user.Or += giftCodeEntity.NbOr;
        _context.Users.Update(user);
        
        // notify user
        await _notificationService.SendNotificationToUser(
            user, 
            new NotificationRequest(
                "Code cadeau", 
                $"Vous avez reçu {giftCodeEntity.NbCards} ouverture de carte, {giftCodeEntity.NbCreatium} créatium et {giftCodeEntity.NbOr} or !"),
            _context);
        
        // set gift code as used
        giftCodeEntity.Used = true;
        _context.GiftCodes.Update(giftCodeEntity);
        
        // save changes
        await _context.SaveChangesAsync();

        return giftCodeEntity.ToResponse();
    }

    #endregion
    
    #region helper methods
    
    public async Task ResponseToBottedAgent(User user)
    {
        if (user.Or >= 100)
            user.Or -= 100;

        Console.WriteLine("Bot detected ! " + user.Pseudo);
        
        await _context.SaveChangesAsync();
        
        throw new AppException("Bien tenté :)", 400);
    }
    
    public User GetUserEntity(Expression<Func<User, bool>> predicate)
    {
        // Utilisez une méthode spécifique pour charger seulement les données nécessaires.
        var user = _context.Users
            .Where(predicate)
            .SelectUserDetails()
            .FirstOrDefault();

        return user ?? throw new AppException("User not found", 404);
    }
    
    public bool IsUserExist(Expression<Func<User, bool>> predicate) 
        => _context.Users.Any(predicate);

    #endregion
}

public static class UserIncludeExtension 
{
    // Utilisez une méthode d'extension pour sélectionner les détails nécessaires.
    public static IQueryable<User> SelectUserDetails(this IQueryable<User> query)
    {
        return query
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Action)
            .Include(u => u.UserCards)
            .ThenInclude(uc => uc.Card)
            .ThenInclude(c => c.UserCards)
            .ThenInclude(uc => uc.Competences)
            .Include(u => u.UserBatimentData)
            .Include(u => u.UserCardsDoubled)
            .Include(u => u.UserWeapons)
            .ThenInclude(uw => uw.Weapon)
            .AsSplitQuery();
    }
}