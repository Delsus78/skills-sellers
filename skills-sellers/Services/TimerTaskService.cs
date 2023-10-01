namespace skills_sellers.Services;

public interface ITimerTaskService
{
    
}
public class TimerTaskService : ITimerTaskService, IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}