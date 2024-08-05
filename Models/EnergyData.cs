using System;
using System.ComponentModel.DataAnnotations;

namespace HomeWizardMonitor.Models;

public class EnergyData
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double ActivePowerW { get; set; }
    public double TotalPowerImportKwh { get; set; }
    public double TotalPowerExportKwh { get; set; }
    public double SelfConsumedSolarKwh { get; set; } 
}
