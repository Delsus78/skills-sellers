using skills_sellers.Entities;
using skills_sellers.Entities.Registres;

namespace skills_sellers.Models.Extensions;

public static class RegistresModelsExtensions
{
    public static UserRegistreInfoResponse ToResponse(this UserRegistreInfo userRegistreInfo, List<Registre> registres) 
        => new(
            userRegistreInfo.HostileAttackWon,
            userRegistreInfo.HostileAttackLost,
            registres.Select(r => r.ToResponse()).ToList()
        );

    public static RegistreResponse ToResponse(this Registre registre) 
        => new(
            registre.Id,
            registre.Name,
            registre.Description,
            registre.EncounterDate,
            registre.Type,
            registre.Type == RegistreType.Hostile ? ((RegistreHostile)registre).CardPower : null,
            registre.Type == RegistreType.Hostile ? ((RegistreHostile)registre).CardWeaponPower : null,
            registre.Type == RegistreType.Friendly ? ((RegistreFriendly)registre).ResourceOffer : null,
            registre.Type == RegistreType.Friendly ? ((RegistreFriendly)registre).ResourceDemand : null,
            registre.Type == RegistreType.Friendly ? ((RegistreFriendly)registre).ResourceOfferAmount : null,
            registre.Type == RegistreType.Friendly ? ((RegistreFriendly)registre).ResourceDemandAmount : null,
            registre.Type == RegistreType.Player ? ((RegistrePlayer)registre).RelatedPlayer.Pseudo : null,
            registre.Type == RegistreType.Player ? ((RegistrePlayer)registre).RelatedPlayerId : null
        );
    
    public static FightReportResponse ToResponse(this FightReport fightReport) 
        => new(
            fightReport.Id,
            fightReport.FightDate,
            fightReport.Description
        );
}