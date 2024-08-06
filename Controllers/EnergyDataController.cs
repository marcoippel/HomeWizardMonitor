using Microsoft.AspNetCore.Mvc;
using HomeWizardMonitor.Services;
using HomeWizardMonitor.Data;
using HomeWizardMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeWizardMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnergyDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static readonly TimeZoneInfo _amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
    private readonly ILogger<EnergyDataController> _logger;

    public EnergyDataController(ApplicationDbContext context, ILogger<EnergyDataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<EnergyData>> GetLatest(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.EnergyData.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        var latestData = await query.OrderByDescending(e => e.Timestamp).FirstOrDefaultAsync();

        if (latestData == null)
            return NotFound();

        return latestData;
    }

    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<EnergyData>>> GetRange(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.EnergyData.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        var rangeData = await query.OrderBy(e => e.Timestamp).ToListAsync();

        if (!rangeData.Any())
            return NotFound();

        return rangeData;
    }

    [HttpGet("period-totals")]
    public async Task<ActionResult<PeriodTotals>> GetPeriodTotals(DateTime startDate, DateTime endDate)
    {
        var startDateAmsterdam = TimeZoneInfo.ConvertTimeFromUtc(startDate, _amsterdamTimeZone);
        var endDateAmsterdam = TimeZoneInfo.ConvertTimeFromUtc(endDate, _amsterdamTimeZone);

        _logger.LogInformation($"Fetching data for period: {startDateAmsterdam} to {endDateAmsterdam}");

        var data = await _context.EnergyData
            .Where(e => e.Timestamp >= startDateAmsterdam && e.Timestamp <= endDateAmsterdam)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        if (!data.Any())
        {
            return NotFound("No data available for the specified period");
        }

        var firstReading = data.First();
        var lastReading = data.Last();

        var totalImported = lastReading.TotalPowerImportKwh - firstReading.TotalPowerImportKwh;
        var totalExported = lastReading.TotalPowerExportKwh - firstReading.TotalPowerExportKwh;

        // Calculate self-consumed solar energy for the selected period
        var periodSelfConsumed = data.Last().SelfConsumedSolarKwh - data.First().SelfConsumedSolarKwh;

        // Calculate total self-consumed solar energy for all time
        var allTimeSelfConsumed = _context.EnergyData.OrderByDescending(x => x.Timestamp).First().SelfConsumedSolarKwh;

        _logger.LogInformation($"Period self-consumed: {periodSelfConsumed}, All-time self-consumed: {allTimeSelfConsumed}");

        // Additional check: if period self-consumed is greater than all-time, log an error
        if (periodSelfConsumed > allTimeSelfConsumed)
        {
            _logger.LogError($"Calculation error: Period self-consumed ({periodSelfConsumed}) is greater than all-time self-consumed ({allTimeSelfConsumed})");
        }

        return new PeriodTotals
        {
            TotalImported = Math.Max(0, totalImported),
            TotalExported = Math.Max(0, totalExported),
            PeriodSelfConsumed = Math.Max(0, periodSelfConsumed),
            AllTimeSelfConsumed = Math.Max(0, allTimeSelfConsumed),
            StartDate = startDateAmsterdam,
            EndDate = endDateAmsterdam
        };
    }
}