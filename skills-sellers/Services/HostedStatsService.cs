using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;

public class HostedStatsService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStatsService _statsService;
    private Timer? _timer;

    public HostedStatsService(IServiceProvider serviceProvider, IStatsService statsService)
    {
        _serviceProvider = serviceProvider;
        _statsService = statsService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // start a timer to save stats every 5 minutes
        _timer = new Timer(SaveStats, null, (int) TimeSpan.Zero.TotalMilliseconds, (int) TimeSpan.FromMinutes(1).TotalMilliseconds);
        return Task.CompletedTask;
    }

    private void SaveStats(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            if (_statsService.Stats.IsEmpty)
                return;
                
            Console.Out.WriteLine($"Saving stats for {_statsService.Stats.Count} users.");
            
            // save stats
            foreach (var (userId, stats) in _statsService.Stats)
            {
                var userStats = context.Stats.FirstOrDefault(s => s.UserId == userId);
                if (userStats is null) // skip if user has no stats
                    continue;
            
                foreach (var (statName, amount) in stats)
                {
                    var propertyInfo = userStats.GetType().GetProperty(statName);
                    if (propertyInfo is null)
                        throw new AppException($"Property {statName} not found in Stats entity", 500);
                    var currentValue = (int) propertyInfo.GetValue(userStats);
                    propertyInfo.SetValue(userStats, currentValue + amount);
                }
            }
            
            // log stats in one line
            var statsString = string.Join(" | ", _statsService.Stats.Select(s => $"{s.Key}: {string.Join(", ", s.Value.Select(v => $"{v.Key}: {v.Value}"))}"));
            Console.Out.WriteLine($"Stats: {statsString}");

            // clear cache
            _statsService.ResetStats();
        
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
        
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // save stats
        SaveStats(null);
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}