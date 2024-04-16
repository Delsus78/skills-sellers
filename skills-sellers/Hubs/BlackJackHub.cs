using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace skills_sellers.Hubs;

[Authorize]
public class BlackJackHub : Hub
{ }