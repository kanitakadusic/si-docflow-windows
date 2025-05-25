using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docflow.Models
{
    public class InfoDev
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Int32 Device { get; set; }

        public InfoDev(string id, string name, Int32 device)
        {
            Id = id;
            Name = name;
            Device = device;//0-camera 1-scanner
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
