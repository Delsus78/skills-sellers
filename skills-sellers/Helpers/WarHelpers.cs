using System.Text;
using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Registres;
using skills_sellers.Models.Extensions;
using Action = System.Action;

namespace skills_sellers.Helpers;

public static class WarHelpers
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="card1"></param>
    /// <param name="card2"></param>
    /// <returns>
    /// 1 = card1 win
    /// -1 = card2 win
    /// 0 = draw 
    /// </returns>
    public static (int result, int pointDiff) Fight(UserCard card1, UserCard card2) =>
        Fight(
            new FightingEntity("card1", card1.ToResponse().Power, card1.UserWeapon?.Affinity),
            new FightingEntity("card2", card2.ToResponse().Power, card2.UserWeapon?.Affinity)
        );

    /// <summary>
    /// 
    /// </summary>
    /// <param name="card1"></param>
    /// <param name="card2"></param>
    /// <returns>
    /// 1 = card1 win
    /// -1 = card2 win
    /// 0 = draw 
    /// </returns>
    private static (int result, int pointDiff) Fight(FightingEntity card1, FightingEntity card2)
    {
        // get the power of each card
        var card1Power = card1.TotalPower;
        var card2Power = card2.TotalPower;

        var affinityWin = CompareAffinity(card1.Affinity, card2.Affinity);

        // compare affinity
        switch (affinityWin)
        {
            case 1:
                card2Power = (int)(card2Power * 0.5);
                break;
            case -1:
                card1Power = (int)(card1Power * 0.5);
                break;
        }

        // get the difference
        var difference = card1Power - card2Power;

        // return the result
        return difference switch
        {
            > 0 => (1, difference),
            < 0 => (-1, difference),
            _ => (0, difference)
        };
    }

    public static List<FightingEntity> SplitArmyFromRegistreHostile(RegistreHostile registreHostile, bool isDefending = false)
    {
        var powerRemaining = (registreHostile.CardPower + registreHostile.CardWeaponPower) * (isDefending ? 5 : 1);
        
        var army = new List<FightingEntity>();
        
        while (powerRemaining > 0)
        {
            var cardPower = Math.Min(powerRemaining, 40);
            powerRemaining -= cardPower;
            army.Add(new FightingEntity("Carte", cardPower, registreHostile.Affinity));
        }
        
        // explique en une phrase comment l'armée a été divisée
        return army;

    }

    public static (bool defenseWin, string fightReport) Battle(IEnumerable<FightingEntity> armyDefense,
        IEnumerable<FightingEntity> armyAttack)
    {
        FightingEntity DamageCardWithPFC(FightingEntity attackingCard2, (int result, int pointDiff) fightResult)
        {
            attackingCard2 = attackingCard2 with
            {
                Name = attackingCard2.Name + "!*-*!",
                TotalPower = Math.Abs(fightResult.pointDiff)
            };
            return attackingCard2;
        }

        FightingEntity PostAttackLooseActionsForFightingEntity(List<FightingEntity> list, FightingEntity fightingEntity1)
        {
            // carte attaque retirée
            list.RemoveAt(0);
            // get next attack card
            if (list.Count > 0)
                fightingEntity1 = list[0];
            return fightingEntity1;
        }

        FightingEntity PostDefenseLooseActionsForFightingEntity(List<FightingEntity> fightingEntities, FightingEntity fightingEntity, FightingEntity attackingCard1)
        {
            // defense card removed
            fightingEntities.RemoveAt(0);

            // calculate power added from defense fail and add the power to the next defense card
            var addedDefensePower = Math.Max(fightingEntity.TotalPower - attackingCard1.TotalPower, 0);

            if (fightingEntities.Count == 0 && addedDefensePower > 0) // ajout d'une carte en plus
            {
                fightingEntities.Add(fightingEntity with
                {
                    Name = fightingEntity.Name + "(*)",
                    TotalPower = addedDefensePower
                });
                fightingEntity = fightingEntities[0];
            }
            else if (addedDefensePower > 0)
            {
                fightingEntity = fightingEntities[0] with
                {
                    Name = fightingEntities[0].Name + $"!*(+{addedDefensePower})*!",
                    TotalPower = fightingEntities[0].TotalPower + addedDefensePower
                };
            }

            return fightingEntity;
        }

        var report = new StringBuilder();

        // new list order by power
        var orderedArmyDefense = armyDefense.OrderByDescending(c => c.TotalPower).ToList();
        var orderedArmyAttack = armyAttack.OrderByDescending(c => c.TotalPower).ToList();
        var fightDone = 0;
        
        // write in the report the number of cards in each army and total power
        report.Append(
            $"[*!DEFENSE!*] *!{orderedArmyDefense.Count}!* cartes avec un total de *!{orderedArmyDefense.Sum(c => c.TotalPower)}!* de puissance\n");
        report.Append(
            $"[*!ATTAQUE!*] *!{orderedArmyAttack.Count}!* cartes avec un total de *!{orderedArmyAttack.Sum(c => c.TotalPower)}!* de puissance\n");

        if (orderedArmyDefense.Count == 0)
        {
            report.Append("[*!DEFENSE!*] *!Aucune!* carte disponible pour défendre => *!ATTAQUE GAGNE !!*\n");
            return (false, report.ToString());
        }
        
        if (orderedArmyAttack.Count == 0)
        {
            report.Append("[*!ATTAQUE!*] *!Aucune!* carte disponible pour attaquer => *!DEFENSE GAGNE !!*\n");
            return (true, report.ToString());
        }
        
        var attackingCard = orderedArmyAttack[0];
        var defendingCard = orderedArmyDefense[0];
        
        while (fightDone == 0)
        {
            // fight
            var fightResult = Fight(defendingCard, attackingCard);
            
            switch (fightResult.result)
            {
                case 1: // defense win
                    report.Append(
                        $"[*!DEFENSE!*] *!{defendingCard.Name}!* (*!{defendingCard.TotalPower}/{defendingCard.Affinity}!*) vs [*!ATTAQUE!*] *!{attackingCard.Name}!* (*!{attackingCard.TotalPower}/{attackingCard.Affinity}!*) => *!DEFENSE GAGNE !!*\n");
                    
                    defendingCard = DamageCardWithPFC(defendingCard, fightResult);
                    attackingCard = PostAttackLooseActionsForFightingEntity(orderedArmyAttack, attackingCard);
                    
                    break;
                
                case -1: // attaque win
                {
                    report.Append(
                        $"[*!DEFENSE!*] *!{defendingCard.Name}!* (*!{defendingCard.TotalPower}/{defendingCard.Affinity}!*) vs [*!ATTAQUE!*] *!{attackingCard.Name}!* (*!{attackingCard.TotalPower}/{attackingCard.Affinity}!*) => *!ATTAQUE GAGNE !!*\n");
                    
                    defendingCard = PostDefenseLooseActionsForFightingEntity(orderedArmyDefense, defendingCard, attackingCard);
                    attackingCard = DamageCardWithPFC(attackingCard, fightResult);

                    break;
                }
                default: // draw
                {
                    report.Append(
                        $"[*!DEFENSE!*] *!{defendingCard.Name}!* (*!{defendingCard.TotalPower}/{defendingCard.Affinity}!*) vs [*!ATTAQUE!*] *!{attackingCard.Name}!* (*!{attackingCard.TotalPower}/{attackingCard.Affinity}!*) => Egalité !!*\n");
                    
                    attackingCard = PostAttackLooseActionsForFightingEntity(orderedArmyAttack, attackingCard);
                    defendingCard = PostDefenseLooseActionsForFightingEntity(orderedArmyDefense, defendingCard, attackingCard);

                    break;
                }
            }

            if (orderedArmyAttack.Count == 0) // si plus de carte attaque, la défense gagne
            {
                report.Append(
                    "[*!ATTAQUE!*] *!Aucune!* carte disponible pour attaquer => *!DEFENSE GAGNE !!*\n");
                fightDone = 1;
            } else if (orderedArmyDefense.Count == 0) // si plus de carte défense mais qu'il reste des cartes attaque, l'attaque gagne
            {
                report.Append(
                    $"[*!DEFENSE!*] *!Aucune!* carte disponible pour défendre contre *!{attackingCard.Name}!* (*!{attackingCard.TotalPower}/{attackingCard.Affinity}!*) => *!ATTAQUE GAGNE !!*\n");
                fightDone = -1;
            }
        }

        return (fightDone > 0, report.ToString());
    }

    public static FightReport GetFightDescription(UserCard card1, UserCard card2, int fightResult)
    {
        // get the power of each card
        var card1Power = card1.ToResponse().Power;
        var card2Power = card2.ToResponse().Power;

        var description = new List<string>
        {
            $"La carte *!{card1.Card.Name}!* de *!{card1.User.Pseudo}!* rencontre *!{card2.Card.Name}!* de *!{card2.User.Pseudo}!* ! \n",
        };

        if (card1.UserWeapon != null)
            description.Add(
                $"La carte *!{card1.Card.Name}!* de *!{card1.User.Pseudo}!* est équipée de *!{card1.UserWeapon.Weapon.Name}!* ! \n");

        if (card2.UserWeapon != null)
            description.Add(
                $"La carte *!{card2.Card.Name}!* de *!{card2.User.Pseudo}!* est équipée de *!{card2.UserWeapon.Weapon.Name}!* ! \n");

        switch (CompareAffinity(card1.UserWeapon?.Affinity, card2.UserWeapon?.Affinity))
        {
            case 0:
                description.Add($"Les affinités des armes s'annulent !\n");

                description.Add($"[*!{card1.User.Pseudo}!*] *!{card1.Card.Name}!* => *!{card1Power}!* de puissance!\n");
                description.Add($"[*!{card2.User.Pseudo}!*] *!{card2.Card.Name}!* => *!{card2Power}!* de puissance!\n");
                break;
            case 1:
                description.Add(
                    $"L'affinité de l'arme de *!{card1.User.Pseudo}!* est supérieure à celle de *!{card2.User.Pseudo}!* !\n");
                description.Add(
                    $"L'affinité *!{card1.UserWeapon?.Affinity}!* fait passer la puissance de *!{card2.User.Pseudo}!*[*!{card2.Card.Name}!*] de *!{card2Power}!* à *!{card2Power * 0.5}!* ! \n");

                description.Add($"[*!{card1.User.Pseudo}!*] *!{card1.Card.Name}!* => *!{card1Power}!* de puissance!\n");
                description.Add(
                    $"[*!{card2.User.Pseudo}!*] *!{card2.Card.Name}!* => *!{card2Power * 0.5}!* de puissance!\n");
                break;
            case -1:
                description.Add(
                    $"L'affinité de l'arme de *!{card1.User.Pseudo}!* est inférieure à celle de *!{card2.User.Pseudo}!* !\n");
                description.Add(
                    $"L'affinité *!{card2.UserWeapon?.Affinity}!* fait passer la puissance de *!{card1.User.Pseudo}!*[*!{card1.Card.Name}!*] de *!{card1Power}!* à *!{card1Power * 0.5}!* !\n");

                description.Add(
                    $"[*!{card1.User.Pseudo}!*] *!{card1.Card.Name}!* => *!{card1Power * 0.5}!* de puissance! \n");
                description.Add($"[*!{card2.User.Pseudo}!*] *!{card2.Card.Name}!* => *!{card2Power}!* de puissance!\n");
                break;
        }

        switch (fightResult)
        {
            case 1: // user win
                description.Add(
                    $"*!{card1.User.Pseudo}!* gagne le combat !\n");
                break;
            case -1: // opponent win
                description.Add(
                    $"*!{card2.User.Pseudo}!* gagne le combat !\n");
                break;
            default:
                description.Add(
                    $"*!{card2.User.Pseudo}!* et *!{card1.User.Pseudo}!* sont à égalité !\n");
                break;
        }

        return new FightReport
        {
            Description = description,
            FightDate = DateTime.Now
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="affinity1"></param>
    /// <param name="affinity2"></param>
    /// <returns>
    /// 1 = affinity1 win
    /// -1 = affinity1 lose
    /// 0 = draw
    /// </returns>
    public static int CompareAffinity(WeaponAffinity? affinity1, WeaponAffinity? affinity2)
    {
        if (affinity1 == null)
            if (affinity2 == null)
                return 0;
            else
                return -1;

        if (affinity2 == null)
            return 1;

        if (affinity1 == affinity2)
            return 0;

        switch (affinity1)
        {
            case WeaponAffinity.Pierre when affinity2 == WeaponAffinity.Ciseaux:
            case WeaponAffinity.Feuille when affinity2 == WeaponAffinity.Pierre:
            case WeaponAffinity.Ciseaux when affinity2 == WeaponAffinity.Feuille:
                return 1;

            case WeaponAffinity.Pierre when affinity2 == WeaponAffinity.Feuille:
            case WeaponAffinity.Feuille when affinity2 == WeaponAffinity.Ciseaux:
            case WeaponAffinity.Ciseaux when affinity2 == WeaponAffinity.Pierre:
                return -1;

            default:
                return 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static string GetRandomWarLoot(User user, int multiplicator, bool isLooser = false, int basePourcentageForRarity = 0)
    {
        // ANY CHANGE HERE NEED TO BE REPLICATED IN THE GETESDTIMATEDWARLOOTFORUSER METHOD IN THE WAR SERVICE
        var random = Randomizer.RandomInt(0, 100) - basePourcentageForRarity;
        multiplicator = multiplicator <= 0 ? 1 : multiplicator;
        switch (random)
        {
            case < 79:
                var amountCreatium = Randomizer.RandomInt(multiplicator * 10, multiplicator * 30);
                var finalAmountCreatium = isLooser ? amountCreatium/3 : amountCreatium;
                user.Creatium += finalAmountCreatium;
                return $"{finalAmountCreatium} créatium [COMMUN]";
            case < 94:
                var amountOr = Randomizer.RandomInt(multiplicator * 15, multiplicator * 20);
                var finalAmountOr = isLooser ? amountOr/3 : amountOr;
                user.Or += finalAmountOr;
                return $"{finalAmountOr} or [RARE]";
            case < 99:
                var amountPack = Randomizer.RandomInt(Math.Max(multiplicator/8, 1), Math.Max(multiplicator/8,3));
                var finalAmountPack = isLooser ? (amountPack/3 <= 0 ? 1 : amountPack/3) : amountPack;
                user.NbCardOpeningAvailable += finalAmountPack;
                return $"{finalAmountPack} packs [EPIC]";
            case < 100:
                user.NbWeaponUpgradeAvailable++;
                return "1 amélioration d'arme [LEGENDAIRE]";
            default:
                throw new AppException("Erreur lors de la récupération du butin de guerre", 500);
        }
    }

    public static RegistreHostile GenerateHostileRegistre(User user, string planetName)
    {
        (int cardPower, int cardWeaponPower) = CalculatePlanetCardPower(
            planetName,
            user.UserCards.Count);

        var registre = new RegistreHostile
        {
            UserId = user.Id,
            EncounterDate = DateTime.Now,
            Name = planetName,
            Description = Randomizer.RandomQuote(planetName),
            CardPower = cardPower,
            CardWeaponPower = cardWeaponPower,
            Affinity = cardWeaponPower > 0 ? Randomizer.RandomWeaponAffinity() : null
        };

        return registre;
    }

    public static RegistreFriendly GenerateFriendlyRegistre(User user, string planetName, int powerLevel)
    {
        (int price, string resourceNameToPay, int winAmount, string resourceNameToWin) =
            GetFriendlyTrade(powerLevel, planetName);

        var registre = new RegistreFriendly
        {
            UserId = user.Id,
            EncounterDate = DateTime.Now,
            Name = planetName,
            Description = Randomizer.RandomQuote(planetName),
            ResourceOffer = resourceNameToWin,
            ResourceDemand = resourceNameToPay,
            ResourceOfferAmount = winAmount,
            ResourceDemandAmount = price
        };

        return registre;
    }

    public static RegistrePlayer GeneratePlayerRegistre(User user, User relatedPlayer, DbSet<Card> cardsDb)
    {
        var registre = new RegistrePlayer
        {
            UserId = user.Id,
            EncounterDate = DateTime.Now,
            RelatedPlayerId = relatedPlayer.Id,
            Name = relatedPlayer.Pseudo,
            Description = cardsDb.GetRandomCardDescription(relatedPlayer.Pseudo + user.Pseudo)
        };

        return registre;
    }

    public static RegistreNeutral GenerateNeutralRegistre(User user, string planetName)
    {
        var registre = new RegistreNeutral
        {
            UserId = user.Id,
            EncounterDate = DateTime.Now,
            Name = planetName,
            Description = Randomizer.RandomDeathQuote(planetName)
        };

        return registre;
    }

    public static (int price, string resourceNameToPay, int winAmount, string resourceNameToWin) GetFriendlyTrade(
        int powerLevel,
        string seed)
    {

        var random = new Random(seed.GetHashCode());

        // Randomly pick a resource to win
        var resources = new List<string> { "creatium", "or", "nourriture" };
        var winIndex = random.Next(resources.Count);
        var resourceNameToWin = resources[winIndex];
        resources.RemoveAt(winIndex);

        var resourceNameToPay = resources[random.Next(resources.Count)];
        var priceAmount = 0;
        var winAmount = 0;

        switch (resourceNameToWin)
        {
            case "nourriture":
                var foodRatio = random.Next(15, 31);
                winAmount = random.Next(
                    Math.Max(powerLevel - 35, 3),
                    Math.Max(powerLevel - 25, 10));
                priceAmount = resourceNameToPay switch
                {
                    "or" => foodRatio * winAmount * 4,
                    "creatium" => foodRatio * winAmount * 7,
                    _ => throw new AppException("Erreur lors de la génération du registre", 500)
                };
                break;
            case "creatium":
                winAmount = powerLevel * random.Next(6, 13);
                priceAmount = resourceNameToPay switch
                {
                    "or" => winAmount / 4,
                    "nourriture" => Math.Max(winAmount / 70, 1),
                    _ => throw new AppException("Erreur lors de la génération du registre", 500)
                };
                break;
            case "or":
                winAmount = powerLevel * random.Next(3, 9);
                priceAmount = resourceNameToPay switch
                {
                    "creatium" => winAmount * 3,
                    "nourriture" => Math.Max(winAmount / 30, 1),
                    _ => throw new AppException("Erreur lors de la génération du registre", 500)
                };
                break;
        }

        return (priceAmount, resourceNameToPay, winAmount, resourceNameToWin);
    }

    public static (int cardPower, int cardWeaponPower) CalculatePlanetCardPower(string planetName, int userTotalCards)
    {
        var random = new Random(planetName.GetHashCode());
        var cardPower = 0;
        var cardWeaponPower = random.Next(0,userTotalCards / 30);

        // total cards
        var totalCardsPower = userTotalCards / 5;
        cardPower += totalCardsPower;

        // random between 1/4 of the power to add or remove
        var randomPower = random.Next(0, cardPower / 4);
        bool add = random.Next(0, 2) == 1;
        if (add)
            cardPower += randomPower;
        else
            cardPower -= randomPower;

        return (Math.Max(cardPower, 1), cardWeaponPower > 0 ? Math.Max(cardWeaponPower + random.Next(-3, 5), 0) : 0);
    }

    public static string LoosingAnAttack(User user, int nbToLoose)
    {
        var randomXUserCards = user.UserCards
            .Where(uc => uc.Competences.Cuisine > 0 || uc.Competences.Charisme > 0 || uc.Competences.Intelligence > 0 ||
                         uc.Competences.Force > 0 || uc.Competences.Exploration > 0)
            .OrderBy(c => Guid.NewGuid()).Take(nbToLoose)
            .ToList();

        var message = "";

        foreach (var userCard in randomXUserCards)
        {
            var competences = new List<Action>();

            if (userCard.Competences.Cuisine > 0)
                competences.Add(() =>
                {
                    userCard.Competences.Cuisine -= 1;
                    message += $"{userCard.Card.Name} a perdu 1 point de cuisine. \r\n";
                });

            if (userCard.Competences.Charisme > 0)
                competences.Add(() =>
                {
                    userCard.Competences.Charisme -= 1;
                    message += $"{userCard.Card.Name} a perdu 1 point de charisme. \r\n";
                });

            if (userCard.Competences.Intelligence > 0)
                competences.Add(() =>
                {
                    userCard.Competences.Intelligence -= 1;
                    message += $"{userCard.Card.Name} a perdu 1 point d'intelligence. \r\n";
                });

            if (userCard.Competences.Force > 0)
                competences.Add(() =>
                {
                    userCard.Competences.Force -= 1;
                    message += $"{userCard.Card.Name} a perdu 1 point de force. \r\n";
                });

            if (userCard.Competences.Exploration > 0)
                competences.Add(() =>
                {
                    userCard.Competences.Exploration -= 1;
                    message += $"{userCard.Card.Name} a perdu 1 point d'exploration. \r\n";
                });

            if (competences.Count <= 0) continue;
            var randomIndex = Randomizer.RandomInt(0, competences.Count);
            competences[randomIndex].Invoke();
        }

        return message;
    }
}

public record FightingEntity(string Name, int TotalPower, WeaponAffinity? Affinity)
{
    public int TotalPower { get; set; } = TotalPower;
}
