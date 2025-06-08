using docflow.Models.ApiModels;
using docflow.Services;
using System.Threading.Tasks;
using System;
using System.IO;

namespace docflow.Utilities
{
    public static class RemoteUtil
    {
        public static async Task HandleRemoteDocumentProcessAsync(RemoteDocumentProcessBody body)
        {
            System.Diagnostics.Debug.WriteLine("RemoteUtil [START HandleRemoteDocumentProcessAsync]");
            System.Diagnostics.Debug.WriteLine(body);
            try
            {
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FileFolder",
                    body.file_name
                );
                if (!File.Exists(filePath))
                {
                    throw new Exception($"File '{body.file_name}' not found");
                }

                ApiService _apiService = new();

                ProcessResponse? response = (await _apiService.ProcessDocumentAsync(
                    filePath,
                    "Remote Initiator",
                    body.document_type_id
                ))?.Data;

                await _apiService.SendProcessResponseAsync(new SendProcessResponseContent
                {
                    Document_Type_Id = int.Parse(body.document_type_id),
                    File_Name = body.file_name,
                    Ocr_Result = response,
                }, body.transaction_id);

                if (response != null)
                {
                    foreach (var ocr in response.Process_Results[0].Ocr)
                    {
                        ocr.Result.Text = string.Empty;
                    }
                    await _apiService.FinalizeDocumentAsync(response);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoteUtil [ERROR HandleRemoteDocumentProcessAsync]: {ex.Message}");
            }
        }
    }
}
