using System.Collections.Generic;

namespace docflow.Models.ApiModels
{
    public class FetchDocumentTypesResponse
    {
        public List<DocumentType>? Data { get; set; } = null;
        public string Message { get; set; } = string.Empty;
    }
}
