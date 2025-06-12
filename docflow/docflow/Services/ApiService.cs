using docflow.Models.ApiModels;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

namespace docflow.Services
{
    public class ApiService
    {
        private static readonly string API_FETCH_DOCUMENT_TYPES = "document/types";
        private static readonly string API_PROCESS_DOCUMENT = "document/process?lang=" + AppSettings.OCR_LANGUAGE + "&engines=" + AppSettings.OCR_ENGINE;
        private static readonly string API_FINALIZE_DOCUMENT = "document/finalize";

        private static readonly string API_SEND_PROCESS_RESPONSE = "remote/result";

        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly Newtonsoft.Json.JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
            }
        };

        public ApiService()
        {
            _httpClient = new HttpClient();
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

        public async Task<FetchDocumentTypesResponse?> FetchDocumentTypesAsync()
        {
            System.Diagnostics.Debug.WriteLine("ApiService [START FetchDocumentTypesAsync]");
            try
            {
                var responseContent = await _httpClient.GetStringAsync(AppSettings.PROCESSING_SERVER_BASE_URL + API_FETCH_DOCUMENT_TYPES);
                return JsonSerializer.Deserialize<FetchDocumentTypesResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiService [ERROR FetchDocumentTypesAsync]: {ex.Message}");
                return null;
            }
        }

        public async Task<ProcessDocumentResponse?> ProcessDocumentAsync(
            string filePath,
            string user,
            string documentTypeId
        )
        {
            System.Diagnostics.Debug.WriteLine("ApiService [START ProcessDocumentAsync]");
            try
            {
                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
                ByteArrayContent fileContent = new(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                using var form = new MultipartFormDataContent
                {
                    { fileContent, "file", Path.GetFileName(filePath) },
                    { new StringContent(user), "user" },
                    { new StringContent(ConfigurationService.CurrentConfig.MachineId), "machineId" },
                    { new StringContent(documentTypeId), "documentTypeId" }
                };

                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_REQ_SENT);

                var apiResponse = await _httpClient.PostAsync(AppSettings.PROCESSING_SERVER_BASE_URL + API_PROCESS_DOCUMENT, form);
                string responseContent = await apiResponse.Content.ReadAsStringAsync();
                var deserializedResponse = JsonSerializer.Deserialize<ProcessDocumentResponse>(responseContent, _jsonOptions);

                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_RESULT_RECEIVED);

                return deserializedResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiService [ERROR ProcessDocumentAsync]: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> FinalizeDocumentAsync(ProcessResponse content)
        {
            System.Diagnostics.Debug.WriteLine("ApiService [START FinalizeDocumentAsync]");
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(content, _jsonSettings);
                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var apiResponse = await _httpClient.PostAsync(AppSettings.PROCESSING_SERVER_BASE_URL + API_FINALIZE_DOCUMENT, body);
                return apiResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiService [ERROR FinalizeDocumentAsync]: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendProcessResponseAsync(SendProcessResponseContent content, string transactionId)
        {
            System.Diagnostics.Debug.WriteLine("ApiService [START SendProcessResponseAsync]");
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(content, _jsonSettings);

                var request = new HttpRequestMessage(HttpMethod.Post, AppSettings.ADMIN_SERVER_BASE_URL + API_SEND_PROCESS_RESPONSE)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("transaction-id", transactionId);

                var apiResponse = await _httpClient.SendAsync(request);
                return apiResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiService [ERROR SendProcessResponseAsync]: {ex.Message}");
                return false;
            }
        }
    }
}
