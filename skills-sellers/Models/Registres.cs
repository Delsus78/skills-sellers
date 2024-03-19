using skills_sellers.Entities;
using skills_sellers.Entities.Registres;

namespace skills_sellers.Models;

public record RegistreResponse(
    int Id,
    string Name,
    string Description,
    DateTime EncounterDate,
    RegistreType Type,
    int? CardPower,
    int? CardWeaponPower,
    WeaponAffinity? Affinity,
    string? ResourceOffer,
    string? ResourceDemand,
    int? ResourceOfferAmount,
    int? ResourceDemandAmount,
    string? RelatedPlayerName,
    int? RelatedPlayerId);

public record UserRegistreInfoResponse(int HostileAttackWon, int HostileAttackLost, List<RegistreResponse> Registres);