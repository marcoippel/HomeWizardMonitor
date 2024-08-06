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

        var previousReading = await GetPreviousReadingAsync();
        double cumulativeSelfConsumedSolar = previousReading?.SelfConsumedSolarKwh ?? 0;

        double incrementalSelfConsumedSolar = 0;
        if (response.active_power_w < 0)
        {
            // Als we exporteren, is al het verbruik van zonne-energie
            incrementalSelfConsumedSolar = Math.Abs(response.active_power_w) / 1000 / 3600; // Converteer W naar kWh per seconde
        }
        else
        {
            // Als we importeren, is het verschil tussen productie en export het zelf verbruik
            if (previousReading != null)
            {
                double currentProduction = Math.Max(0, response.total_power_export_t1_kwh + response.total_power_export_t2_kwh -
                                                    (previousReading.TotalPowerExportKwh));
                incrementalSelfConsumedSolar = Math.Max(0, currentProduction - (Math.Abs(response.active_power_w) / 1000 / 3600));
            }
        }

        cumulativeSelfConsumedSolar += incrementalSelfConsumedSolar;

        var energyData = new EnergyData
        {
            Timestamp = DateTime.UtcNow,
            ActivePowerW = response.active_power_w,
            TotalPowerImportKwh = response.total_power_import_t1_kwh + response.total_power_import_t2_kwh,
            TotalPowerExportKwh = response.total_power_export_t1_kwh + response.total_power_export_t2_kwh,
            SelfConsumedSolarKwh = cumulativeSelfConsumedSolar
        };

        _logger.LogInformation($"Collected data: Active Power: {energyData.ActivePowerW}W, " +
                               $"Import: {energyData.TotalPowerImportKwh}kWh, " +
                               $"Export: {energyData.TotalPowerExportKwh}kWh, " +
                               $"Cumulative Self Consumed: {energyData.SelfConsumedSolarKwh}kWh, " +
                               $"Incremental Self Consumed: {incrementalSelfConsumedSolar}kWh");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.EnergyData.Add(energyData);
        await dbContext.SaveChangesAsync();
    }

    private async Task<EnergyData> GetPreviousReadingAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.EnergyData.OrderByDescending(e => e.Timestamp).FirstOrDefaultAsync();
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