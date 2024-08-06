namespace HomeWizardMonitor.Models
{
    public class PeriodTotals
    {
        public double TotalImported { get; set; }
        public double TotalExported { get; set; }
        public double PeriodSelfConsumed { get; set; }
        public double AllTimeSelfConsumed { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
