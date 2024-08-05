using HomeWizardMonitor.Models;

namespace HomeWizardMonitor.Services;

public interface IHomeWizardService
{
    Task<EnergyData?> GetLatestDataAsync();
    Task<IEnumerable<EnergyData>> GetDataForLastHourAsync();
}