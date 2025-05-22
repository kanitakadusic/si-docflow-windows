using System;

namespace docflow.Models
{
    public enum OperationalMode
    {
        Headless,
        Standalone
    }

    public class ApplicationConfig
    {
        public int Id { get; set; }
        public string Title { get; set; } = "Docflow Client";
        public string Location { get; set; } = "Unknown";
        public string MachineId { get; set; } = AppConfig.machine_id;
        public string OperationalMode { get; set; } = "headless";
        public int PollingFrequency { get; set; } = 1; // Default 1 hour
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime LastFetched { get; set; } = DateTime.Now;
        public bool IsConfigured { get; set; } = false;
    }
}
