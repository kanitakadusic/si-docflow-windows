using System;
using System.IO;
using System.Threading.Tasks;
using docflow.Models.ApiModels;

namespace docflow.Services
{
    public static class DocumentProcessingService
    {
        public static async Task ProcessRemoteCommand(CommandListenerService.RemoteCommand command)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"START | transaction ID: {command.transaction_id}, document type: {command.document_type_id}, file name: {command.file_name}");

                ApiService _apiService = new();

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FileFolder",
                    command.file_name
                );
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"File not found: {command.file_name}");
                    return;
                }

                ProcessResponse result = (await _apiService.ProcessDocumentAsync(
                    filePath,
                    "Remote Initiator",
                    command.document_type_id
                ))?.Data ?? new ProcessResponse();

                await _apiService.SendProcessResponseAsync(result, command.transaction_id);

                foreach (var ocr in result.Process_Results[0].Ocr)
                {
                    ocr.Result.Text = string.Empty;
                }
                await _apiService.FinalizeDocumentAsync(result);

                System.Diagnostics.Debug.WriteLine($"END | transaction ID: {command.transaction_id}, document type: {command.document_type_id}, file name: {command.file_name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Processing failed: {ex.Message}");
            }
        }
    }
}