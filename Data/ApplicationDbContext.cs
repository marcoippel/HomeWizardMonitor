using Microsoft.EntityFrameworkCore;
using HomeWizardMonitor.Models;

namespace HomeWizardMonitor.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EnergyData> EnergyData => Set<EnergyData>();
}