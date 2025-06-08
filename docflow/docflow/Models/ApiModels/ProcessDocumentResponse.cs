namespace docflow.Models.ApiModels
{
    public class ProcessDocumentResponse
    {
        public ProcessResponse? Data { get; set; } = null;
        public string Message { get; set; } = string.Empty;
    }
}
