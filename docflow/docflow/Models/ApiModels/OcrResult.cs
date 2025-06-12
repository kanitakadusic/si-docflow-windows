namespace docflow.Models.ApiModels
{
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; } = -1.0;
        public double Price { get; set; } = -1.0;
    }
}
