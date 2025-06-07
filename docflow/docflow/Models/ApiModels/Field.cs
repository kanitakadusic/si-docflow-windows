using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class Field
    {
        public string Name { get; set; } = string.Empty;
        public List<double> Upper_Left { get; set; } = [];
        public List<double> Lower_Right { get; set; } = [];
        public bool Is_Multiline { get; set; } = false;
    }
}
