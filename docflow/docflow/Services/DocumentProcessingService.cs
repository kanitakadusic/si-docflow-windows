using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using docflow.Models;

namespace docflow.Services
{
    public static class DocumentProcessingService
    {
        private static readonly ApplicationConfig _currentConfig = new ApplicationConfig();

        public static async Task ProcessRemoteCommand(CommandListenerService.RemoteCommand command)
        {
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FileFolder",
                command.file_name
            );

            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"File not found: {command.file_name}");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing document: {command.file_name}, Type: {command.document_type_id}, Transaction: {command.transaction_id}");

                // Log that we're starting to process this command
                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_REQ_SENT);

                // Process with existing server
                var result = await ProcessDocumentAsync(filePath, command.document_type_id);

                // Log that we received processing results
                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_RESULT_RECEIVED);

                // Send results back to the admin server
                await SendResultsAsync(command.transaction_id, command.document_type_id, command.file_name, result);

                System.Diagnostics.Debug.WriteLine($"Document processing completed for transaction: {command.transaction_id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Processing failed: {ex.Message}");
            }
        }
        private static async Task<JObject> ProcessDocumentAsync(string filePath, string documentTypeId)
        {
            System.Diagnostics.Debug.WriteLine($"Sending document to processing server: {filePath}");

            // Reuse existing processing logic
            using var client = new HttpClient();
            using var form = new MultipartFormDataContent();

            // Add file content
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            string mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            // Add the same parameters as in the UI version
            form.Add(new StringContent("Headless User"), "user");
            form.Add(new StringContent(_currentConfig.MachineId), "machineId");
            form.Add(new StringContent(documentTypeId), "documentTypeId");

            // Use the same URL as in the MainWindow.xaml.cs
            var response = await client.PostAsync(
                string.Concat(AppSettings.PROCESSING_SERVER_BASE_URL, "document/process?lang=" + AppSettings.OCR_LANGUAGE + "&engines=" + AppSettings.OCR_ENGINE),
                form
            );

            var responseContent = await response.Content.ReadAsStringAsync();

            // Ispiši kompletan odgovor servera u Debug konzolu
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("============ PROCESSING SERVER RESPONSE ============");
            System.Diagnostics.Debug.WriteLine(responseContent);
            System.Diagnostics.Debug.WriteLine("=====================================================");
            System.Diagnostics.Debug.WriteLine("");

            System.Diagnostics.Debug.WriteLine($"Received processing response with status: {response.StatusCode}");

            return JObject.Parse(responseContent);
        }
        private static async Task SendResultsAsync(string transactionId, string docTypeId, string fileName, JObject result)
        {
            System.Diagnostics.Debug.WriteLine($"Sending processing results for transaction: {transactionId}");

            // Create a finalized data object similar to what the ProcessResults.xaml.cs sends
            var finalizedData = new JObject
            {
                ["document_type_id"] = int.Parse(docTypeId),
                ["engine"] = AppSettings.OCR_ENGINE,
                ["ocr"] = result["data"]?[0]?["ocr"]
            };

            using var client = new HttpClient();

            // First, send the results to the admin server (as we're currently doing)
            var adminPayload = new JObject
            {
                ["document_type_id"] = docTypeId,
                ["file_name"] = fileName,
                ["ocr_result"] = result["data"]
            };

            var adminRequest = new HttpRequestMessage(HttpMethod.Post,
                string.Concat(AppSettings.ADMIN_SERVER_BASE_URL,"remote/result"))
            {
                Content = new StringContent(adminPayload.ToString(), System.Text.Encoding.UTF8, "application/json")
            };
            adminRequest.Headers.Add("transaction-id", transactionId);

            var adminResponse = await client.SendAsync(adminRequest);
            System.Diagnostics.Debug.WriteLine($"Results sent to admin server with status: {adminResponse.StatusCode}");

            // Then, also finalize the document with the document server (as in ProcessResults.xaml.cs)
            var docServerContent = new StringContent(
                finalizedData.ToString(),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var docServerResponse = await client.PostAsync(
                string.Concat(AppSettings.PROCESSING_SERVER_BASE_URL, "document/finalize"),
                docServerContent
            );

            System.Diagnostics.Debug.WriteLine($"Document finalized with status: {docServerResponse.StatusCode}");
        }

        private static string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}