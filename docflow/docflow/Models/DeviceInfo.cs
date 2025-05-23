using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docflow.Models
{
    public class DeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Int32 Device { get; set; }

        // Opciono: Konstruktor za lakše kreiranje objekata
        public DeviceInfo(string id, string name, Int32 device)
        {
            Id = id;
            Name = name;
            Device = device;
        }
        public override string ToString()
        {
            return Name; // Vraća samo naziv za prikaz u ComboBoxu
        }
    }
}
