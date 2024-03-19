using skills_sellers.Entities;
using skills_sellers.Models.Users;

namespace skills_sellers.Models;

public record WarCreationRequest(
    List<int> RegistreAllysId, 
    int? RegistreTargetId, 
    List<int> UserCardsIds,
    int? InvitedWarId,
    bool? IsInvited = false);
public record WarEstimationResponse(
    bool IsWarPossible, 
    string Message,
    DateTime? EstimatedDuration = null,
    Dictionary<string,string>? Couts = null);
public record AddCardsToWarRequest(List<int> UserCardIds);

public record WarResponse(
    int Id, 
    RegistreResponse RegistreTarget,
    List<UserResponse> UserAllies,
    WarStatus Status, 
    DateTime CreatedAt, 
    UserResponse UserCreator,
    bool isInvitationPending);