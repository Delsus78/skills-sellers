using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Actions;
using skills_sellers.Entities.Registres;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;
using skills_sellers.Services.ActionServices;

namespace skills_sellers.Services;

public interface IWarService
{
    Task StartWar(User starter, WarCreationRequest model);
    Task CancelWar(User starter, int warId);
    Task<WarEstimationResponse> EstimateWar(User user, WarCreationRequest model);
    Task<WarResponse> AcceptWar(User user, int warId, AddCardsToWarRequest model);
    Task DeclineWar(User user, int warId);
    Task<WarResponse?> GetActualWar(User user);
    Task<WarResponse?> GetInvitedWar(User user);
    Task StartBattle(int warId, DataContext context);
    Task GiveRandomWarLoot(int userId, int multiplicator);
    WarLootEstimationResponse GetEstimatedWarLootForUser(int multiplicator, int reducedPourcentage);
    WarSimulationResponse SimulateWar(WarSimulationRequest model);
}
public class WarService : IWarService
{
    private readonly DataContext _context;
    private readonly IActionTaskService _actionTaskService;
    private readonly INotificationService _notificationService;
    private readonly IStatsService _statsService;

    public WarService(DataContext context, 
        IActionTaskService actionTaskService, 
        INotificationService notificationService, 
        IStatsService statsService)
    {
        _context = context;
        _actionTaskService = actionTaskService;
        _notificationService = notificationService;
        _statsService = statsService;
    }

    public async Task StartWar(User starter, WarCreationRequest model)
    {
        var registreTarget = _context.Registres
            .Where(r => r.UserId == starter.Id)
            .FirstOrDefault(r => r.Id == model.RegistreTargetId);
        if (registreTarget == null)
            throw new AppException("Cible non trouvée", 404);
        
        var userAllyIds = _context.Registres
            .Where(r => r.UserId == starter.Id && model.RegistreAllysId.Contains(r.Id))
            .OfType<RegistrePlayer>()
            .Select(r => r.RelatedPlayerId).ToList();
        if (userAllyIds.Count != model.RegistreAllysId.Count)
            throw new AppException("Un des alliés n'a pas été trouvé", 404);
        
        var validation = CanStartWar(starter, model.UserCardsIds, userAllyIds, registreTarget);
        if (!validation.valid)
            throw new AppException(validation.why, 400);
        
        // create war
        var war = new War
        {
            UserId = starter.Id,
            UserAllyIds = userAllyIds,
            UserTargetId = registreTarget is RegistrePlayer rp ? rp.RelatedPlayerId : null,
            Status = WarStatus.EnAttente,
            CreatedAt = DateTime.Now,
            RegistreTargetId = registreTarget.Id
        };

        var warEntity = _context.Wars.Add(war).Entity;
        await _context.SaveChangesAsync();

        // start warAction for user cards
        await _actionTaskService.CreateNewActionAsync(starter.Id, new ActionRequest
        {
            ActionName = "guerre",
            CardsIds = model.UserCardsIds,
            WarId = warEntity.Id
        });
        
        // send notification to alliés
        var alliesName = new List<string>();
        foreach (var allyId in userAllyIds)
        {
            var ally = _context.Users.Find(allyId);
            if (ally == null)
                throw new AppException("Un des alliés n'a pas été trouvé", 404);
            
            alliesName.Add(ally.Pseudo);
            
            await _notificationService.SendWarNotificationToUser(ally, 
                new NotificationRequest("[GUERRE] - Invitation", 
                    $"Vous avez été invité à une guerre par {starter.Pseudo} contre {registreTarget.Name} !\r\n La guerre commence dans 30 minutes !",
                    ""), _context);
        }

        await _notificationService.SendNotificationToUser(starter, 
            new NotificationRequest("[GUERRE] - en attente", 
                $"Votre guerre contre {registreTarget.Name} à bien été enregistrée, la bataille aura lieu dans 30 minutes !",
                "cards"), _context);
        
        // send notification to target
        if (registreTarget is RegistrePlayer registrePlayer)
        {
            await _context.Entry(registrePlayer).Reference(r => r.RelatedPlayer).LoadAsync();
            await _notificationService.SendWarNotificationToUser(registrePlayer.RelatedPlayer, 
                new NotificationRequest("[GUERRE] - Déclaration", 
                    $"Le joueur {starter.Pseudo} vous a déclaré la guerre !\r\n" +
                    $"Il a invité les joueurs {string.Join(", ", alliesName)}" +
                    $"\r\n La bataille aura lieu dans 30 minutes ! Alors préparez vos satellites !",
                    ""), _context);
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelWar(User starter, int warId)
    {
        var war = await _context.Wars.FindAsync(warId);
        if (war == null)
            throw new AppException("Guerre non trouvée", 404);
        
        if (war.UserId != starter.Id)
            throw new AppException("Vous n'êtes pas le créateur de cette guerre", 400);
        
        if (war.Status != WarStatus.EnAttente)
            throw new AppException("La guerre n'est pas en attente", 400);
        
        // remove action
        var action = _context.Actions
            .OfType<ActionGuerre>()
            .FirstOrDefault(a => a.WarId == war.Id);
        if (action == null)
            throw new AppException("Action non trouvée", 404);
        
        await _actionTaskService.DeleteActionAsync(starter.Id, action.Id);
        war.Status = WarStatus.Annulee;
        
        // send notification to alliés
        var allies = _context.Users
            .Where(u => war.UserAllyIds.Contains(u.Id))
            .ToList();
        
        foreach (var ally in allies)
        {
            await _notificationService.SendNotificationToUser(ally, 
                new NotificationRequest("[GUERRE] - Annulation", 
                    $"La guerre contre {war.User.Pseudo} a été annulée !",
                    "cards"), _context);
        }
        
        await _notificationService.SendNotificationToUser(starter, 
            new NotificationRequest("[GUERRE] - Annulation", 
                $"La guerre contre {war.User.Pseudo} a été annulée !",
                "cards"), _context);
        
        // send notification to target
        if (war.UserTargetId != null)
        {
            var target = _context.Users.Find(war.UserTargetId);
            if (target == null)
                throw new AppException("Cible non trouvée", 404);
            
            await _notificationService.SendNotificationToUser(target, 
                new NotificationRequest("[GUERRE] - Annulation", 
                    $"La guerre contre {war.User.Pseudo} a été annulée !", ""), _context);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<WarEstimationResponse> EstimateWar(User user, WarCreationRequest model)
    {
        if (model.IsInvited.HasValue && model.IsInvited.Value)
        {
            var estimationForInvited = CanStartWarAsInvited(user, model.InvitedWarId, model.UserCardsIds);
            return new WarEstimationResponse(
                estimationForInvited.valid, 
                estimationForInvited.why, 
                estimationForInvited.estimatedDuration, 
                estimationForInvited.couts);
        }
        var registreTarget = await _context.Registres.FindAsync(model.RegistreTargetId);
        if (registreTarget == null)
            return new WarEstimationResponse(false,"Cible non trouvée");
        
        var userAllyIds = _context.Registres
            .Where(r => r.UserId == user.Id && model.RegistreAllysId.Contains(r.Id))
            .OfType<RegistrePlayer>()
            .Select(r => r.RelatedPlayerId).ToList();
        
        if (userAllyIds.Count != model.RegistreAllysId.Count)
            return new WarEstimationResponse(false, "Un des alliés n'a pas été trouvé");
        
        var estimation = CanStartWar(user, model.UserCardsIds, userAllyIds, registreTarget);
        
        return new WarEstimationResponse(estimation.valid, estimation.why, estimation.estimatedDuration, estimation.couts);
    }
    
    public async Task<WarResponse> AcceptWar(User user, int warId, AddCardsToWarRequest model)
    {
        var validation = CanStartWarAsInvited(user, warId, model.UserCardIds);
        if (!validation.valid)
            throw new AppException(validation.why, 400);
        
        user.Nourriture -= model.UserCardIds.Count * 4;
        
        // add card to the action
        var action = _context.Actions
            .OfType<ActionGuerre>()
            .FirstOrDefault(a => a.WarId == warId);
        if (action == null)
            throw new AppException("Action non trouvée", 404);

        var cards = _context.Entry(user).Collection(u => u.UserCards).Query()
            .Where(uc => model.UserCardIds.Contains(uc.CardId))
            .ToList();
        action.UserCards.AddRange(cards);

        // notify starter
        var war = await _context.Wars.FindAsync(warId);
        if (war == null)
            throw new AppException("Guerre non trouvée", 404);
        
        var starter = await _context.Users.FindAsync(war.UserId);
        if (starter == null)
            throw new AppException("Créateur de la guerre non trouvé", 404);
        
        await _notificationService.SendNotificationToUser(starter,
            new NotificationRequest("[GUERRE] - Acceptation", 
                $"{user.Pseudo} a accepté votre invitation à la guerre !", ""), _context);
        
        await _notificationService.SendNotificationToUser(user,
            new NotificationRequest("[GUERRE] - Acceptation", 
                $"{starter.Pseudo} a été notifié de votre acceptation à la guerre !", "cards"), _context);
        
        await _context.SaveChangesAsync();

        return await GetInvitedWar(user) ?? throw new AppException("Auncune guerre en cours.", 404);
    }

    public async Task DeclineWar(User user, int warId)
    {
        var war = await _context.Wars.FindAsync(warId);
        if (war == null)
            throw new AppException("Guerre non trouvée", 404);
        
        if (!war.UserAllyIds.Contains(user.Id))
            throw new AppException("Vous n'êtes pas invité à cette guerre", 400);
        
        war.UserAllyIds.Remove(user.Id);
        
        // notify starter
        var starter = await _context.Users.FindAsync(war.UserId);
        if (starter == null)
            throw new AppException("Créateur de la guerre non trouvé", 404);
        
        await _notificationService.SendNotificationToUser(starter,
            new NotificationRequest("[GUERRE] - Refus", 
                $"{user.Pseudo} a refusé votre invitation à la guerre !", ""), _context);
        
        await _context.SaveChangesAsync();
    }

    public async Task<WarResponse?> GetActualWar(User user)
    {
        var actualWars = _context.Wars
            .Include(w => w.User)
            .Where(w => w.UserAllyIds.Contains(user.Id) || w.UserId == user.Id)
            .Where(w => w.Status == WarStatus.EnCours || w.Status == WarStatus.EnAttente)
            .ToList();
        
        if (actualWars.Count == 0)
            return null;
        
        var war = actualWars[0];
        
        var allies = _context.Users
            .Where(u => war.UserAllyIds.Contains(u.Id))
            .ToList();

        var registreTarget = await _context.Registres.FindAsync(war.RegistreTargetId);
        if (registreTarget is RegistrePlayer rPl)
            await _context.Entry(rPl).Reference(rPl => rPl.RelatedPlayer).LoadAsync();

        return war.ToWarResponse(registreTarget?.ToResponse(), allies);
    }
    
    public async Task<WarResponse?> GetInvitedWar(User user)
    {
        var actionGuerres = user.UserCards
            .Where(uc => uc.Action is ActionGuerre)
            .Select(uc => uc.Action as ActionGuerre)
            .ToList();
        
        var actualWars = _context.Wars
            .Include(w => w.User)
            .Where(w => w.UserAllyIds.Contains(user.Id))
            .Where(w => w.Status == WarStatus.EnAttente)
            .ToList();

        var isInvitationPending = actualWars.Count > 0 &&
                                  actionGuerres.Count(a => actualWars.Select(w => w.Id).Contains(a.WarId)) == 0;

        if (actualWars.Count == 0)
            return null;
        
        var war = actualWars[0];
        
        var allies = _context.Users
            .Where(u => war.UserAllyIds.Contains(u.Id))
            .ToList();

        var registreTarget = await _context.Registres.FindAsync(war.RegistreTargetId);
        if (registreTarget == null)
            throw new AppException("Cible non trouvée", 404);

        if (registreTarget is RegistrePlayer rPl)
            await _context.Entry(rPl).Reference(rPl => rPl.RelatedPlayer).LoadAsync();

        return war.ToWarResponse(registreTarget.ToResponse(), allies, isInvitationPending);
    }

    public async Task StartBattle(int warId, DataContext context)
    {
        var war = context.Wars
            .Include(w => w.User)
            .FirstOrDefault(w => w.Id == warId);
        if (war == null)
            throw new AppException("Guerre non trouvée", 404);
        
        var starter = war.User;
        
        
        // remove allies from war who didnt accept (no cards in action)
        var allies = _context.Users
            .Where(u => war.UserAllyIds.Contains(u.Id))
            .Where(u => u.UserCards.Any(uc => 
                uc.Action is ActionGuerre 
                && ((ActionGuerre) uc.Action).WarId == warId))
            .ToList();
        war.UserAllyIds.RemoveAll(id => !allies.Select(a => a.Id).Contains(id));

        // set bouclier et clear ceux des alliés
        starter.WarTimeout = null;
        foreach (var ally in allies)
            ally.WarTimeout = null;

        var attackingCards = context.Actions
            .Where(a => a is ActionGuerre && ((ActionGuerre)a).WarId == warId)
            .SelectMany(a => a.UserCards)
            .Select(uc => new FightingEntity(
                uc.Card.Name,
                uc.ToResponse().Power,
                uc.UserWeapon != null ? uc.UserWeapon.Affinity : null))
            .ToList();

        var defendingCards = new List<FightingEntity>();

        string targetName;
        if (war.UserTargetId != null)
        {
            var target = await _context.Users.FindAsync(war.UserTargetId);
            if (target == null)
                throw new AppException("Cible non trouvée", 404);

            targetName = target.Pseudo;
            target.WarTimeout = DateTime.Now.AddDays(3);
            
            defendingCards = context.Actions
                .Where(a => a.UserId == target.Id && a is ActionSatellite)
                .SelectMany(a => a.UserCards)
                .Include(uc => uc.Card)
                .Include(uc => uc.Competences)
                .Include(uc => uc.UserWeapon)
                .ThenInclude(w => w.Weapon)
                .Select(uc => new FightingEntity(
                    uc.Card.Name, 
                    uc.ToResponse().Power,
                    uc.UserWeapon != null ? uc.UserWeapon.Affinity : null))
                .ToList();
            
            // x3 power for the target
            defendingCards.ForEach(dc => dc.TotalPower *= 3);
        }
        else // is a PNJ
        {
            var registre = await _context.Registres.FindAsync(war.RegistreTargetId);
            if (registre is not RegistreHostile registreHostile)
                throw new AppException("Cible non trouvée", 404);

            targetName = registreHostile.Name;
            
            defendingCards.AddRange(WarHelpers.SplitArmyFromRegistreHostile(registreHostile, true));
        }
        
        // BATTLE
        var results = WarHelpers.Battle(defendingCards, attackingCards);
        results.fightReport += results.defenseWin
            ? "[*!GUERRE!*] - *!Victoire de la défense !!*\n"
            : "[*!GUERRE!*] - *!Victoire de l'attaque !!*\n";

        // adding fightReport
        var fightDesc = new List<string>
        {
            $"[*!GUERRE!*] - *!{war.User.Pseudo}!* " 
            + (allies.Count > 0 ? "(*!" + string.Join(", ", allies.Select(a => a.Pseudo)) + "!*) " : "") 
            + $"attaque *!{targetName}!* - {DateTime.Now}"
        };
        fightDesc.AddRange(results.fightReport.Split("\n").ToList());
        context.FightReports.Add(new FightReport
        {
            Description = fightDesc,
            FightDate = DateTime.Now
        });

        #region rewards
        if (results.defenseWin)
        {
            if (war.UserTargetId != null)
            {
                // defense rewards
                var target = await _context.Users.FindAsync(war.UserTargetId);
                if (target == null)
                    throw new AppException("Cible non trouvée", 404);

                target.Score += 500;
                
                var notifMsg = "Voici votre récompense pour avoir survécu !\n";

                var averageTotalCards 
                    = ((starter.UserCards.Count + allies.Sum(a => a.UserCards.Count))/2 + target.UserCards.Count)/2;
                
                notifMsg += WarHelpers.GetRandomWarLoot(target, averageTotalCards) + "\n";
                notifMsg += WarHelpers.GetRandomWarLoot(target, averageTotalCards) + "\n";
                
                await _notificationService.SendNotificationToUser(target, new NotificationRequest(
                    $"[GUERRE] - Victoire de la défense contre {war.User.Pseudo}", 
                    notifMsg + "+ 500 SCORE\n",""), context);
                
                // attack loose
                var looseMsg = "Les cartes suivantes ont perdu 1 point de compétence : \r\n";
                looseMsg += WarHelpers.LoosingAnAttack(war.User, attackingCards.Count);
                war.User.Score += 100;
                await _notificationService.SendNotificationToUser(war.User, new NotificationRequest(
                    $"[GUERRE] - Défaite contre {target.Pseudo}", 
                    looseMsg + "+ 100 SCORE\n", "cards"), context);
                
                foreach (var ally in allies)
                {
                    var loosingAllyMsg = "Les cartes suivantes ont perdu 1 point de compétence : \r\n";
                    loosingAllyMsg += WarHelpers.LoosingAnAttack(ally, attackingCards.Count);
                    ally.Score += 150;
                    await _notificationService.SendNotificationToUser(ally, new NotificationRequest(
                        $"[GUERRE] - Défaite contre {target.Pseudo}", 
                        loosingAllyMsg + "+ 150 SCORE\n", "cards"), context);
                }
            } 
            
            // nothing to loose vs a pnj
        }
        else
        {
            if (war.UserTargetId != null)
            {
                var target = await _context.Users.FindAsync(war.UserTargetId);
                if (target == null)
                    throw new AppException("Cible non trouvée", 404);
                
                var creatium = target.Creatium / 3;
                var or = target.Or / 3;
                var nourriture = target.Nourriture / 3;
                
                // attack rewards
                var rewardedCreatiumPerAlly = creatium / (allies.Count + 1);
                var rewardedOrPerAlly = or / (allies.Count + 1);
                var rewardedNourriturePerAlly = nourriture / (allies.Count + 1);
                
                starter.Creatium += rewardedCreatiumPerAlly;
                starter.Or += rewardedOrPerAlly;
                starter.Nourriture += rewardedNourriturePerAlly;
                starter.Score += 400;
                
                var notifMsg = $"Voici votre récompense pour avoir vaincu à {allies.Count + 1} joueur(s) !\n" 
                               + $"Nourriture + {rewardedNourriturePerAlly}\n" 
                               + $"Or + {rewardedOrPerAlly}\n" 
                               + $"Créatium + {rewardedCreatiumPerAlly}\n";
                
                await _notificationService.SendNotificationToUser(starter, new NotificationRequest(
                    $"[GUERRE] - Victoire contre {target.Pseudo}", 
                    notifMsg + "+ 400 SCORE\n", "cards"), context);

                foreach (var ally in allies)
                {
                    ally.Creatium += rewardedCreatiumPerAlly;
                    ally.Or += rewardedOrPerAlly;
                    ally.Nourriture += rewardedNourriturePerAlly;
                    ally.Score += 300;
                    await _notificationService.SendNotificationToUser(ally, new NotificationRequest(
                        $"[GUERRE] - Victoire contre {target.Pseudo}", 
                        notifMsg + "+ 300 SCORE\n", "cards"), context);
                }
                
                // defense loose
                target.Creatium -= creatium;
                target.Or -= or;
                target.Nourriture -= nourriture;
                target.Score += 200;
                var loosingMsg = "Voici ce que vous avez perdu :(\n"
                    + $"Nourriture - {nourriture}\n"
                    + $"Or - {or}\n"
                    + $"Créatium - {creatium}\n"
                    + "+ 200 SCORE\n";
                
                // defense consolation
                loosingMsg += $"Voici votre consolation pour avoir perdu !\n{
                    WarHelpers.GetRandomWarLoot(target, attackingCards.Count, true, 3)}\n";
                
                await _notificationService.SendNotificationToUser(target, new NotificationRequest(
                    $"[GUERRE] - Défaite contre {war.User.Pseudo}",
                    loosingMsg, ""), context);
            }
            else
            {
                var registreTarget = await _context.Registres.FindAsync(war.RegistreTargetId);
                
                if (registreTarget is not RegistreHostile registreHostile)
                    throw new AppException("Cible non trouvée", 404);
                
                // based on planet hostile total power
                var multiplicator = (registreHostile.CardPower + registreHostile.CardWeaponPower) * 5;
                
                // deleting registre and all registres with a name containing the same name (ex: "Tue" will delete "Tueur", "Tueur de la mort", etc)
                
                var registresToDelete = _context.Registres.Where(r => r.Name.ToUpper().Contains(registreTarget.Name.ToUpper())).ToList();
                
                foreach (var registre in registresToDelete)
                {
                    // notify user
                    var correspondingUser = await _context.Users.FindAsync(registre.UserId);
                    if (correspondingUser == null) continue;
                    await _notificationService.SendNotificationToUser(correspondingUser,
                        new NotificationRequest(
                            $"[GUERRE] - Victoire de {starter.Pseudo} contre {registre.Name}",
                            $"Votre planète hostile {registre.Name} n'existe donc plus !\n", "buildings"), context);
                }
                
                _context.Registres.RemoveRange(registresToDelete);
                
                foreach (var ally in allies)
                {
                    ally.Score += 100;
                    var notifMsg = "Voici votre récompense pour avoir vaincu !\n";

                    notifMsg += WarHelpers.GetRandomWarLoot(ally, multiplicator) + "\n";
                    notifMsg += WarHelpers.GetRandomWarLoot(ally, multiplicator) + "\n";

                    await _notificationService.SendNotificationToUser(ally, new NotificationRequest(
                        $"[GUERRE] - Victoire contre {registreTarget.Name}", 
                        notifMsg + "+100 SCORE", "cards"), context);
                }
                
                starter.Score += 100;
                
                var notifStarterMsg = "Voici votre récompense pour avoir vaincu !\n";
                notifStarterMsg += WarHelpers.GetRandomWarLoot(starter, multiplicator) + "\n";
                notifStarterMsg += WarHelpers.GetRandomWarLoot(starter, multiplicator) + "\n";
                
                await _notificationService.SendNotificationToUser(starter, new NotificationRequest(
                    $"[GUERRE] - Victoire contre {registreTarget.Name}", 
                    notifStarterMsg + "+100 SCORE", "cards"), context);
                
            }
        }
        #endregion 
        
        // stat
        _statsService.OnPlanetAttacked(starter.Id);
        
        // notify all
        await _notificationService.SendNotificationToAll(new NotificationRequest(
                $"[GUERRE] Guerre de {starter.Pseudo} !",
                "Une guerre vient de se terminée, un nouveau rapport de combat est disponible !", ""),
            context);
    }

    public Task GiveRandomWarLoot(int userId, int multiplicator)
    {
        var user = _context.Users
            .SelectUserDetails().FirstOrDefault(u => u.Id == userId);

        var stringReward = WarHelpers.GetRandomWarLoot(user, multiplicator);
        
        _notificationService.SendNotificationToUser(user, new NotificationRequest(
            "[WarLoot] - Récompense", 
            stringReward, "cards"), _context);
        
        return _context.SaveChangesAsync();
    }

    public WarLootEstimationResponse GetEstimatedWarLootForUser(int multiplicator, int reducedPourcentage)
    {
        var estimations = new Dictionary<string, string>();
        var finalMultiplicator = multiplicator <= 0 ? 1 : multiplicator;
        
        string GetLootRange(int index)
        {
            return index switch
            {
                0 => finalMultiplicator * 10 + " - " + finalMultiplicator * 30 + " Creatium",
                1 => finalMultiplicator * 15 + " - " + finalMultiplicator * 20 + " Or",
                2 => Math.Max(multiplicator / 4, 1) + " - " + Math.Max(multiplicator / 2, 3) + " Packs",
                3 => "1 Amélioration d'arme",
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }
        
        var pourcentages = new List<int> { 79, 15, 5, 1 };

        for (var index = 0; index < pourcentages.Count; index++)
        {
            var pourcentage = pourcentages[index];
            var finalPourcentage = pourcentage - reducedPourcentage;
            if (finalPourcentage <= 0)
                continue;

            estimations.Add(finalPourcentage + "% : ", GetLootRange(index));
        }

        return new WarLootEstimationResponse(estimations);
    }

    public WarSimulationResponse SimulateWar(WarSimulationRequest model)
    {
        var attackingCards = model.Attackers;
        var fakeHostileRegistre = new RegistreHostile
        {
            CardPower = model.Hostile.CardPower,
            CardWeaponPower = model.Hostile.WeaponPower,
            Name = model.Hostile.Name,
            Affinity = model.Hostile.Affinity
        };

        var defendingCards = WarHelpers.SplitArmyFromRegistreHostile(fakeHostileRegistre, true);

        var results = WarHelpers.Battle(defendingCards, attackingCards);
        results.fightReport += results.defenseWin
            ? "[*!GUERRE!*] - *!Victoire de la défense !!*\n"
            : "[*!GUERRE!*] - *!Victoire de l'attaque !!*\n";

        var multiplicator = fakeHostileRegistre.CardPower + fakeHostileRegistre.CardWeaponPower;
        var estimatedLoot = GetEstimatedWarLootForUser(multiplicator, 0);

        return new WarSimulationResponse(results.fightReport, estimatedLoot);
    }

    // get if cards aren't in actions using the classic Estimation Methods for ActionGuerre
    // check if registreTarget is not protected by a war timeout
    // check if registreAlly is not already in a war
    // check if user has done his 2 wars this week already
    private (bool valid, string why, DateTime? estimatedDuration, Dictionary<string,string>? couts) CanStartWar(
        User user, List<int> userCardsIds, List<int> userAllyIds, Registre registreTarget)
    {
        // is cards estimation valid
        var actionEstimation = _actionTaskService.EstimateAction(user.Id, new ActionRequest
        {
            ActionName = "guerre",
            CardsIds = userCardsIds
        });

        if (actionEstimation.Error != null)
            return (false, actionEstimation.Error, null, null);

        // no duplicated ids
        if (userCardsIds.Distinct().Count() != userCardsIds.Count)
            return (false, "Vous ne pouvez pas utiliser deux fois la même carte", null, null);
        if (userAllyIds.Distinct().Count() != userAllyIds.Count)
            return (false, "Vous ne pouvez pas utiliser deux fois le même allié", null, null);
        if (userAllyIds.Contains(user.Id))
            return (false, "Vous ne pouvez pas vous ajouter en tant qu'allié", null, null);

        // minimum 1 card
        if (userCardsIds.Count < 1)
            return (false, "Vous devez utiliser au moins une carte", null, null);
        
        // is user in war
        if (_context.Wars.Where(w => w.Status == WarStatus.Finie && w.Status == WarStatus.Annulee)
            .AsEnumerable()
            .Any(w => w.UserId == user.Id 
                     || w.UserAllyIds.Contains(user.Id) 
                     || w.UserTargetId == user.Id))
              return (false, "Vous êtes déjà en guerre", null, null);

        if (registreTarget is RegistrePlayer registrePlayer)
        {
            // an ally is the target
            if (userAllyIds.Contains(registrePlayer.UserId))
                return (false, "Votre allié ne peux pas être la cible de votre guerre", null, null);

            // war timeout
            if (registrePlayer.User.WarTimeout > DateTime.Now)
                return (false, "La cible est protégée par un temps de guerre", null, null);
            
            // if started vs registrePlayer, user has done his 2 wars 
            if (_context.Wars.Where(w => w.Status != WarStatus.Annulee && w.UserId == user.Id)
                    .AsEnumerable()
                    .Count(w => w.CreatedAt.EstDansSemaineActuelle() && w.UserTargetId != null) >= 2)
                return (false, "Vous avez déjà fait vos 2 guerres de la semaine", null, null);
        }
        
        // deja effectué une guerre aujourd'hui
        if (_context.Wars.Where(w => w.Status != WarStatus.Annulee && w.UserId == user.Id)
                .AsEnumerable()
                .Count(w => w.CreatedAt.EstDansLajourneeActuelle()) >= 1)
            return (false, "Vous avez déjà fait une guerre aujourd'hui", null, null);
        
        // si registre hostile, impossible si le registre a ete rencontré il y a moins de 2 jours
        if (registreTarget is RegistreHostile registreHostile && registreHostile.EncounterDate > DateTime.Now.AddDays(-2)) 
            return (false, "Vous avez déjà rencontré cette planète hostile il y a moins de 2 jours", null, null);

        // ally in war
        if (_context.Wars
            .Where(w => w.Status != WarStatus.Finie && w.Status != WarStatus.Annulee)
            .Any(w =>
                w.UserAllyIds.Any(id => userAllyIds.Contains(id))
                || userAllyIds.Contains(w.UserId)
                || (w.UserTargetId.HasValue && userAllyIds.Contains(w.UserTargetId.Value))
            )
           )
              return (false, "Un de vos alliés est déjà en guerre", null, null);

        return (true, "", actionEstimation.EndDates.First(), actionEstimation.Couts);
    }

    private (bool valid, string why, DateTime? estimatedDuration, Dictionary<string, string>? couts)
        CanStartWarAsInvited(User user, int? warId, List<int> userCardsIds)
    {
        if (!warId.HasValue)
            return (false,"Auncune guerre en cours", null, null);
        
        // is cards estimation valid
        var actionEstimation = _actionTaskService.EstimateAction(user.Id, new ActionRequest
        {
            ActionName = "guerre",
            CardsIds = userCardsIds
        });

        if (actionEstimation.Error != null)
            return (false, actionEstimation.Error, null, null);

        // use warId to find actionId and then add cards to action
        var war = _context.Wars.Find(warId);
        if (war == null)
            return (false, "Guerre non trouvée", null, null);
        
        if (war.Status != WarStatus.EnAttente)
            return (false, "La guerre n'est pas en attente", null, null);
        
        if (!war.UserAllyIds.Contains(user.Id))
            return (false, "Vous n'êtes pas invité à cette guerre", null, null);
        
        // no duplicated ids
        if (userCardsIds.Distinct().Count() != userCardsIds.Count)
            return (false, "Vous ne pouvez pas utiliser deux fois la même carte", null, null);
        
        // minimum 1 card
        if (userCardsIds.Count < 1)
            return (false, "Vous devez utiliser au moins une carte", null, null);

        return (true, "", actionEstimation.EndDates.First(), actionEstimation.Couts);
    }
}
