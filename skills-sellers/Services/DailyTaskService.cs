using Microsoft.EntityFrameworkCore;
using skills_sellers.Entities;
using skills_sellers.Helpers.Bdd;

namespace skills_sellers.Services;


public interface IDailyTaskService
{
    Task ExecuteDailyTaskAsync();
    
    Task DailyResetCuisineAsync();
}
public class DailyTaskService : IDailyTaskService
{
    private readonly DataContext _context;
    
    public DailyTaskService(DataContext context)
    {
        _context = context;
    }

    public async Task ExecuteDailyTaskAsync()
    {
        var today = DateTime.Today;
        var logEntry = await _context.DailyTaskLog.SingleOrDefaultAsync(e => e.ExecutionDate == today);
        if (logEntry == null)
        {
            // Execute the task
            await DailyResetCuisineAsync();

            // Log the execution
            _context.DailyTaskLog.Add(new DailyTaskLog { ExecutionDate = today, IsExecuted = true });
            await _context.SaveChangesAsync();
        }
    }

    public async Task DailyResetCuisineAsync()
    {
        var usersBatimentsData = await _context.UserBatiments.ToListAsync();
        
        foreach (var userBatimentData in usersBatimentsData)
        {
            userBatimentData.NbCuisineUsedToday = 0;
        }
        
        await _context.SaveChangesAsync();
    }
}