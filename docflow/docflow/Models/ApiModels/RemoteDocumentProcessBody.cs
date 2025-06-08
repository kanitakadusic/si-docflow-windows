namespace docflow.Models.ApiModels
{
    public class RemoteDocumentProcessBody
    {
        public string transaction_id { get; set; } = string.Empty;
        public string document_type_id { get; set; } = string.Empty;
        public string file_name { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{{ Transaction_Id: {transaction_id}, Document_Type_Id: {document_type_id}, File_Name: {file_name} }}";
        }
    }
}
