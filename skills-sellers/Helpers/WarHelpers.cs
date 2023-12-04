using skills_sellers.Entities;
using skills_sellers.Models.Extensions;

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
    public static (int result, int difference) Fight(UserCard card1, UserCard card2)
    {
        // get the power of each card
        var card1Power = card1.ToResponse().Power;
        var card2Power = card2.ToResponse().Power;
        
        // compare affinity
        switch (CompareAffinity(card1.UserWeapon?.Affinity, card2.UserWeapon?.Affinity))
        {
            case 0:
                card2Power = (int) (card2Power * 0.5);
                break;
            case 1:
                card1Power = (int) (card1Power * 0.5);
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
                var amountCreatium = Randomizer.RandomInt(4000, 5001);
                user.Creatium += (int) (amountCreatium * (isOpponent ? 0.5 : 1));
                return $"{amountCreatium} créatium";
            case < 94:
                var amountOr = Randomizer.RandomInt(6000, 10001);
                user.Or += (int) (amountOr * (isOpponent ? 0.5 : 1));
                return $"{amountOr} or";
            case < 99:
                var amountPack = Randomizer.RandomInt(5, 16);
                user.NbCardOpeningAvailable += (int) (amountPack * (isOpponent ? 0.5 : 1));
                return $"{amountPack} packs";
            case < 100:
                user.NbWeaponUpgradeAvailable++;
                return "1 amélioration d'arme";
            default:
                throw new AppException("Erreur lors de la récupération du butin de guerre", 500);
        }
    }
}