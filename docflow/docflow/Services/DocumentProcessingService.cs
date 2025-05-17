// DocumentProcessingService.cs
using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using docflow.Services;
using System.Threading.Tasks;

namespace docflow.Services
{
    public static class DocumentProcessingService
    {
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
                // Process with existing server
                var result = await ProcessDocumentAsync(filePath, command.document_type_id);
                
                // Send results
                await SendResultsAsync(command.transaction_id, command.document_type_id, command.file_name, result);
                
                await ClientLogService.LogActionAsync(ClientActionType.COMMAND_RECEIVED);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Processing failed: {ex.Message}");
            }
        }

        private static async Task<JObject> ProcessDocumentAsync(string filePath, string documentTypeId)
        {
            // Reuse existing processing logic
            using var client = new HttpClient();
            using var form = new MultipartFormDataContent();
            
            // Add file content
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            form.Add(new StringContent("kanita123"), "machineId");
            form.Add(new StringContent(documentTypeId), "documentTypeId");

            var response = await client.PostAsync(
                "https://docflow-server.up.railway.app/document/process?lang=bos&engines=tesseract", 
                form
            );

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        private static async Task SendResultsAsync(string transactionId, string docTypeId, string fileName, JObject result)
        {
            using var client = new HttpClient();
            var payload = new JObject
            {
                ["document_type_id"] = docTypeId,
                ["file_name"] = fileName,
                ["ocr_result"] = result["data"]
            };

            var request = new HttpRequestMessage(HttpMethod.Post, 
                "https://docflow-admin.up.railway.app/api/remote/result")
            {
                Content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("transaction-id", transactionId);

            await client.SendAsync(request);
        }
    }
}