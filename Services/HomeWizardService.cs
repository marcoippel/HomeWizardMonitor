using HomeWizardMonitor.Data;
using HomeWizardMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeWizardMonitor.Services;

public class HomeWizardService : IHomeWizardService
{
    private readonly ApplicationDbContext _context;

    public HomeWizardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EnergyData?> GetLatestDataAsync()
    {
        return await _context.EnergyData.OrderByDescending(d => d.Timestamp).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EnergyData>> GetDataForLastHourAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        return await _context.EnergyData
            .Where(d => d.Timestamp >= oneHourAgo)
            .OrderBy(d => d.Timestamp)
            .ToListAsync();
    }
}