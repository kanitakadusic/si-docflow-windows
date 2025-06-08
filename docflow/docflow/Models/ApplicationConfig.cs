using System;

namespace docflow.Models
{
    public class ApplicationConfig
    {
        public int Id { get; set; } = -1;
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string MachineId { get; set; } = AppSettings.MACHINE_ID;
        public string OperationalMode { get; set; } = AppSettings.OPERATIONAL_MODE;
        public int PollingFrequency { get; set; } = AppSettings.POLLING_FREQUENCY; 
        public DateTime LastFetched { get; set; } = DateTime.Now;
        public bool IsConfigured { get; set; } = false;
    }
}