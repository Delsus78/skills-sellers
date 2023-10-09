using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using skills_sellers.Helpers;

namespace skills_sellers.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        catch (Exception ex)
        {
            throw new AppException("Error while connecting to the notification hub : " + ex.Message, 500);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        catch (Exception ex)
        {
            throw new AppException("Error while disconnecting from the notification hub : " + ex.Message, 500);
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    private string GetUserId()
    {
        var userId = Context?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId ?? throw new AppException("User authenticated not found", 400);
    }

}
