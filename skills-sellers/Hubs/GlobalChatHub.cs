using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using skills_sellers.Helpers;
using skills_sellers.Services;

namespace skills_sellers.Hubs;

[Authorize]
public class GlobalChatHub : Hub
{
    private readonly IServiceProvider _serviceProvider;
    
    // nombre de personnes connectées
    public static int UserConnected { get; set; } = 0;
    
    public GlobalChatHub(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public async Task SendMessage(string? username, string message)
    {
        username ??= Context?.User?.Identity?.Name;
        await Clients.All.SendAsync("ReceiveMessage", username, message);
        
        var statsService = _serviceProvider.GetRequiredService<IStatsService>();
        statsService.OnMessageSent(
            int.Parse(
                Context?.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? throw new AppException("User authenticated not found", 400)));
    }
    
    // gestion du nombre de personnes connectées
    public override async Task OnConnectedAsync()
    {
        UserConnected++;
        await Clients.All.SendAsync("UserConnected", UserConnected);

        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        UserConnected--;
        await Clients.All.SendAsync("UserConnected", UserConnected);

        await base.OnDisconnectedAsync(exception);
    }
}