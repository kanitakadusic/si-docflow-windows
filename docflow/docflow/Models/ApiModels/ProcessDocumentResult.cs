using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class ProcessDocumentResult
    {
        public int Document_Type_Id { get; set; } = -1;
        public string Engine { get; set; } = string.Empty;
        public List<MappedOcrResult> Ocr { get; set; } = [];
        public List<int> Triplet_Ids { get; set; } = [];
    }
}
