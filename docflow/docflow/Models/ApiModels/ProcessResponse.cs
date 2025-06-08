using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class ProcessResponse
    {
        public int Document_Type_Id { get; set; } = -1;
        public List<ProcessResult> Process_Results { get; set; } = [];
    }
}
