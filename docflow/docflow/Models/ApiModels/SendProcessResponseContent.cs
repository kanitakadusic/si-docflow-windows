namespace docflow.Models.ApiModels
{
    public class SendProcessResponseContent
    {
        public int Document_Type_Id { get; set; } = -1;
        public string File_Name { get; set; } = string.Empty;
        public ProcessResponse? Ocr_Result { get; set; } = new ProcessResponse();
    }
}
