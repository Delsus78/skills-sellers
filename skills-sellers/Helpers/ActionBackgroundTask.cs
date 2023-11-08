using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Helpers;

public class ActionBackgroundTask
{
    private readonly IServiceProvider _serviceProvider;

    public ActionBackgroundTask(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task RunAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
    }
}
