using System;

namespace docflow.Models
{
    public class ApplicationConfig
    {
        public int Id { get; set; }
        public string Title { get; set; } = AppConfig.AppTitle;
        public string Location { get; set; } = AppConfig.defaultLocation;
        public string MachineId { get; set; } = AppConfig.machine_id;
        public string OperationalMode { get; set; } = AppConfig.DefaultWorkingMode;
        public int PollingFrequency { get; set; } = AppConfig.defaultPollingFrequency; 
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime LastFetched { get; set; } = DateTime.Now;
        public bool IsConfigured { get; set; } = false;
    }
}
