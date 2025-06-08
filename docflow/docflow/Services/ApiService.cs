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

        public ApiService(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
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
            try
            {
                var responseContent = await _httpClient.GetStringAsync(API_FETCH_DOCUMENT_TYPES);
                return JsonSerializer.Deserialize<FetchDocumentTypesResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching document types: {ex.Message}");
                return null;
            }
        }

        public async Task<ProcessDocumentResponse?> ProcessDocumentAsync(
            string filePath,
            string user,
            string machineId,
            string documentTypeId
        )
        {
            try
            {
                string mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
                ByteArrayContent fileContent = new(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                using var form = new MultipartFormDataContent
                {
                    { fileContent, "file", Path.GetFileName(filePath) },
                    { new StringContent(user), "user" },
                    { new StringContent(machineId), "machineId" },
                    { new StringContent(documentTypeId), "documentTypeId" }
                };

                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_REQ_SENT);

                var apiResponse = await _httpClient.PostAsync(API_PROCESS_DOCUMENT, form);
                string responseContent = await apiResponse.Content.ReadAsStringAsync();
                var deserializedResponse = JsonSerializer.Deserialize<ProcessDocumentResponse>(responseContent, _jsonOptions);

                await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_RESULT_RECEIVED);

                return deserializedResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing document: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> FinalizeDocumentAsync(ProcessDocumentResult request)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(request, _jsonSettings);
                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var apiResponse = await _httpClient.PostAsync(API_FINALIZE_DOCUMENT, body);
                return apiResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finalizing document: {ex.Message}");
                return false;
            }
        }
    }
}
