using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace skills_sellers.Hubs;

[Authorize]
public class GlobalChatHub : Hub
{
    public async Task SendMessage(string? username, string message)
    {
        username ??= Context.User.Identity.Name;
        await Clients.All.SendAsync("ReceiveMessage", username, message);
    }
}