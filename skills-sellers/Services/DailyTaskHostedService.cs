namespace skills_sellers.Services;

public class DailyTaskHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    
    public DailyTaskHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Execute the task one time at startup
        ExecuteTask(null);
        
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        var initialDelay = (midnight - now).TotalMilliseconds;
        _timer = new Timer(ExecuteTask, null, (int)initialDelay, (int)TimeSpan.FromDays(1).TotalMilliseconds);
        return Task.CompletedTask;
    }
    
    private async void ExecuteTask(object? state)
    {
        Console.WriteLine("Starting Execution of daily tasks...");
        
        using var scope = _serviceProvider.CreateScope();
        var dailyTaskService = scope.ServiceProvider.GetRequiredService<IDailyTaskService>();
        await dailyTaskService.ExecuteDailyTaskAsync();
        Console.WriteLine("DailyTask done.");
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}