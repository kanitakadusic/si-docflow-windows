namespace docflow.Models.ApiModels
{
    public class MappedOcrResult
    {
        public Field Field { get; set; } = new Field();
        public OcrResult Result { get; set; } = new OcrResult();
    }
}
