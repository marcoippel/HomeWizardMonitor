using Microsoft.AspNetCore.Mvc;
using HomeWizardMonitor.Services;

namespace HomeWizardMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnergyDataController : ControllerBase
{
    private readonly IHomeWizardService _homeWizardService;

    public EnergyDataController(IHomeWizardService homeWizardService)
    {
        _homeWizardService = homeWizardService;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestData()
    {
        var data = await _homeWizardService.GetLatestDataAsync();
        return data is not null ? Ok(data) : NotFound();
    }

    [HttpGet("lasthour")]
    public async Task<IActionResult> GetLastHourData()
    {
        var data = await _homeWizardService.GetDataForLastHourAsync();
        return Ok(data);
    }
}