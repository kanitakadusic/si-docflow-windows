using System;

namespace docflow.Models
{
    public class ApplicationConfig
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Location { get; set; } = "";
        public string MachineId { get; set; } = AppConfig.MACHINE_ID;
        public string OperationalMode { get; set; } = AppConfig.OPERATIONAL_MODE;
        public int PollingFrequency { get; set; } = AppConfig.POLLING_FREQUENCY; 
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime LastFetched { get; set; } = DateTime.Now;
        public bool IsConfigured { get; set; } = false;
    }
}