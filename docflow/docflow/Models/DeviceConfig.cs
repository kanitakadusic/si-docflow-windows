namespace docflow.Models
{
    public enum DeviceType
    {
        Camera = 0,
        Scanner = 1
    }

    public class DeviceConfig(string id, string name, DeviceType device)
    {
        public string Id { get; set; } = id;
        public string Name { get; set; } = name;
        public DeviceType Device { get; set; } = device;

        public override string ToString()
        {
            return Name;
        }
    }
}
