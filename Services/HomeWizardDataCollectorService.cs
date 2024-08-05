using System.Net.Http.Json;
using HomeWizardMonitor.Data;
using HomeWizardMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeWizardMonitor.Services;

public class HomeWizardDataCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HomeWizardDataCollectorService> _logger;
    private readonly HomeWizardSettings _settings;
    private readonly HttpClient _httpClient;

    public HomeWizardDataCollectorService(
        IServiceProvider serviceProvider,
        ILogger<HomeWizardDataCollectorService> logger,
        IOptions<HomeWizardSettings> settings,
        HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndSaveDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while collecting data from HomeWizard P1 meter");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.UpdateIntervalSeconds), stoppingToken);
        }
    }

    private async Task CollectAndSaveDataAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<HomeWizardApiResponse>($"http://{_settings.IpAddress}/api/v1/data");
        if (response is null)
        {
            _logger.LogWarning("Received null response from HomeWizard P1 meter");
            return;
        }

        double totalImport = response.total_power_import_t1_kwh + response.total_power_import_t2_kwh;
        double totalExport = response.total_power_export_t1_kwh + response.total_power_export_t2_kwh;

        // Bereken zelf verbruikte zonne-energie
        double selfConsumedSolar = 0;
        if (response.active_power_w < 0)
        {
            // Als we exporteren, is al het verbruik van zonne-energie
            selfConsumedSolar = Math.Abs(response.active_power_w) / 1000; // Converteer W naar kWh
        }
        else
        {
            // Als we importeren, is het verschil tussen productie en export het zelf verbruik
            double currentProduction = Math.Max(0, totalExport - (await GetPreviousTotalExportAsync()));
            selfConsumedSolar = Math.Max(0, currentProduction - (Math.Abs(response.active_power_w) / 1000));
        }

        var energyData = new EnergyData
        {
            Timestamp = DateTime.UtcNow,
            ActivePowerW = response.active_power_w,
            TotalPowerImportKwh = totalImport,
            TotalPowerExportKwh = totalExport,
            SelfConsumedSolarKwh = selfConsumedSolar
        };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.EnergyData.Add(energyData);
        await dbContext.SaveChangesAsync();
    }

    private async Task<double> GetPreviousTotalExportAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lastEntry = await dbContext.EnergyData
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();
        return lastEntry?.TotalPowerExportKwh ?? 0;
    }
}

public class HomeWizardApiResponse
{
    public double active_power_w { get; set; }
    public double total_power_import_t1_kwh { get; set; }
    public double total_power_import_t2_kwh { get; set; }
    public double total_power_export_t1_kwh { get; set; }
    public double total_power_export_t2_kwh { get; set; }
}

public class HomeWizardSettings
{
    public required string IpAddress { get; set; }
    public int UpdateIntervalSeconds { get; set; }
}