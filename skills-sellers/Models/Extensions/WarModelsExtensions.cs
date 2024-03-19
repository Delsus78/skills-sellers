using skills_sellers.Entities;
using skills_sellers.Models.Users;

namespace skills_sellers.Models.Extensions;

public static class WarModelsExtensions
{
    public static WarResponse ToWarResponse(this War war, 
        RegistreResponse registreTarget, 
        List<User> userAllies,
        bool isInvitationPending = false)
    {
        return new WarResponse(
            war.Id,
            registreTarget,
            userAllies.Select(u => u.ToResponse()).ToList(),
            war.Status,
            war.CreatedAt,
            war.User.ToResponse(),
            isInvitationPending
        );
    }
}