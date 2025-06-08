using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class ProcessDocumentResponse
    {
        public List<ProcessDocumentResult>? Data { get; set; } = null;
        public string Message { get; set; } = string.Empty;
    }
}
