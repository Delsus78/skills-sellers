using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Entities.Registres;
using skills_sellers.Models.Extensions;
using skills_sellers.Services;

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
    public static (int result, int affinityWin) Fight(UserCard card1, UserCard card2)
    {
        // get the power of each card
        var card1Power = card1.ToResponse().Power;
        var card2Power = card2.ToResponse().Power;

        var affinityWin = CompareAffinity(card1.UserWeapon?.Affinity, card2.UserWeapon?.Affinity);
        
        // compare affinity
        switch (affinityWin)
        {
            case 1:
                card2Power = (int) (card2Power * 0.5);
                break;
            case -1:
                card1Power = (int) (card1Power * 0.5);
                break;
        }

        // get the difference
        var difference = card1Power - card2Power;
        
        // return the result
        return difference switch
        {
            > 0 => (1, affinityWin),
            < 0 => (-1, affinityWin),
            _ => (0, affinityWin)
        };
    }

    public static FightReport GetFightDescription(UserCard card1, UserCard card2)
    {
        // get the power of each card
        var card1Power = card1.ToResponse().Power;
        var card2Power = card2.ToResponse().Power;
        var result = Fight(card1, card2).result;

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
                description.Add($"L'affinité de l'arme de *!{card1.User.Pseudo}!* est supérieure à celle de *!{card2.User.Pseudo}!* !\n");
                description.Add(
                    $"L'affinité *!{card1.UserWeapon?.Affinity}!* fait passer la puissance de *!{card2.User.Pseudo}!*[*!{card2.Card.Name}!*] de *!{card2Power}!* à *!{card2Power * 0.5}!* ! \n");

                description.Add($"[*!{card1.User.Pseudo}!*] *!{card1.Card.Name}!* => *!{card1Power}!* de puissance!\n"); 
                description.Add($"[*!{card2.User.Pseudo}!*] *!{card2.Card.Name}!* => *!{card2Power * 0.5}!* de puissance!\n");
                break;
            case -1:
                description.Add($"L'affinité de l'arme de *!{card1.User.Pseudo}!* est inférieure à celle de *!{card2.User.Pseudo}!* !\n");
                description.Add(
                    $"L'affinité *!{card2.UserWeapon?.Affinity}!* fait passer la puissance de *!{card1.User.Pseudo}!*[*!{card1.Card.Name}!*] de *!{card1Power}!* à *!{card1Power * 0.5}!* !\n");

                description.Add($"[*!{card1.User.Pseudo}!*] *!{card1.Card.Name}!* => *!{card1Power * 0.5}!* de puissance! \n");
                description.Add($"[*!{card2.User.Pseudo}!*] *!{card2.Card.Name}!* => *!{card2Power}!* de puissance!\n");
                break;
        }

        switch (result)
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
    public static string GetRandomWarLoot(User user, bool isOpponent = false)
    {
        var random = Randomizer.RandomInt(0, 100);

        switch (random)
        {
            case < 79:
                var amountCreatium = Randomizer.RandomInt(2000, 5001);
                user.Creatium += (int) (amountCreatium * (isOpponent ? 0.5 : 1));
                return $"{amountCreatium} créatium [COMMUN]";
            case < 94:
                var amountOr = Randomizer.RandomInt(1000, 3001);
                user.Or += (int) (amountOr * (isOpponent ? 0.5 : 1));
                return $"{amountOr} or [RARE]";
            case < 99:
                var amountPack = Randomizer.RandomInt(5, 16);
                user.NbCardOpeningAvailable += (int) (amountPack * (isOpponent ? 0.5 : 1));
                return $"{amountPack} packs [EPIC]";
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
            user.Creatium, 
            user.Or, 
            user.UserCards.Count,
            user.UserWeapons.Count,
            user.UserBatimentData.SatelliteLevel);
        
        var registre = new RegistreHostile
        {
            UserId = user.Id,
            EncounterDate = DateTime.Now,
            Name = planetName,
            Description = Randomizer.RandomQuote(planetName),
            CardPower = cardPower,
            CardWeaponPower = cardWeaponPower
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

        int f(double x)
        {
            double a = 9.0 / 49.0;
            double b = 40.0 / 49.0;
            return (int) (a * x + b);
        }
        
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
    
    public static (int cardPower, int cardWeaponPower) CalculatePlanetCardPower(
        string planetName,
        int userCreatium,
        int userOr,
        int userTotalCards,
        int userTotalWeapons,
        int userTotalSatellites)
    {
        var cardPower = 0;
        var cardWeaponPower = (int) CalculateExponentialFunction(userTotalCards);
        var random = new Random(planetName.GetHashCode());
        
        // creatium
        var creatiumPower = (int) (userCreatium / 20000 * 1.1);
        cardPower += creatiumPower;
        
        // or
        var orPower = (int) (userOr / 5000 * 1.1);
        cardPower += orPower;

        // total cards
        var totalCardsPower = (int) (userTotalCards / 20 * 1.2);
        cardPower += totalCardsPower;
        
        // total weapons
        var totalWeaponsPower = (int) (userTotalWeapons / 2 * 1.2);
        cardPower += totalWeaponsPower;
        cardWeaponPower += totalWeaponsPower / 2;
        
        // total satellites
        var totalSatellitesPower = (int) (userTotalSatellites / 2 * 1.2);
        cardPower += totalSatellitesPower;
        cardWeaponPower += totalSatellitesPower / 2;
        
        // random between 1/4 of the power to add or remove
        var randomPower = random.Next(0, cardPower / 4);
        bool add = random.Next(0, 2) == 1;
        if (add)
            cardPower += randomPower;
        else
            cardPower -= randomPower;
        
        return (Math.Max(cardPower, 0), cardWeaponPower);
    }
    
    private static double CalculateExponentialFunction(double x, double croissanceExpo = 1.01, int modulo = 20)
    {
        const double a = 2.117; // Constante a déterminée précédemment
        var b = croissanceExpo; // Croissance exponentielle lente
        const double c = 0.1; // Augmentation à chaque multiple de 20

        return a * Math.Pow(b, x) * (1 + c * Math.Floor(x / modulo));
    }
}