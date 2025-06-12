using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class ProcessResult
    {
        public string Engine { get; set; } = string.Empty;
        public List<MappedOcrResult> Ocr { get; set; } = [];
        public List<int> Triplet_Ids { get; set; } = [];
    }
}
