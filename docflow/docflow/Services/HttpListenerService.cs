using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using docflow.Services;
using docflow.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace docflow.Services
{
    /// <summary>
    /// Service that listens for HTTP requests to process documents remotely
    /// </summary>
    public static class HttpListenerService
    {
        private static HttpListener _listener;
        private static CancellationTokenSource _cancellationTokenSource;
        private static bool _isRunning;
        private const int DEFAULT_PORT = 8080;
        private static string _urlPrefix = $"http://*:{DEFAULT_PORT}/";
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Starts the HTTP listener service
        /// </summary>
        public static async Task StartAsync()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                    return;

                try
                {
                    // Try to create and start HttpListener
                    try
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        _listener = new HttpListener();
                        _listener.Prefixes.Add(_urlPrefix);
                        _listener.Start();
                        _isRunning = true;

                        System.Diagnostics.Debug.WriteLine($"HTTP Listener started on {_urlPrefix}");
                    }
                    catch (HttpListenerException ex)
                    {
                        // If we get an access denied, let's try with localhost only
                        if (ex.ErrorCode == 5) // Access denied
                        {
                            System.Diagnostics.Debug.WriteLine("Access denied for *:8080, trying localhost instead");
                            _urlPrefix = $"http://localhost:{DEFAULT_PORT}/";
                            _listener = new HttpListener();
                            _listener.Prefixes.Add(_urlPrefix);
                            _listener.Start();
                            _isRunning = true;
                            System.Diagnostics.Debug.WriteLine($"HTTP Listener started on {_urlPrefix} (localhost only)");
                            System.Diagnostics.Debug.WriteLine("WARNING: Only local connections will work. For remote access, run as administrator and use 'netsh http add urlacl url=http://*:8080/ user=Everyone'");
                        }
                        else
                        {
                            throw; // Re-throw if it's another error
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start HTTP Listener: {ex.Message}");
                    throw;
                }
            }

            // Display network information to help with troubleshooting
            LogNetworkInfo();

            // Start processing requests
            await ProcessRequestsAsync(_cancellationTokenSource.Token);
        }

        private static void LogNetworkInfo()
        {
            try
            {
                // Get local IP addresses
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                System.Diagnostics.Debug.WriteLine("Network Information for Port Forwarding:");
                System.Diagnostics.Debug.WriteLine($"Computer Name: {hostName}");
                System.Diagnostics.Debug.WriteLine("Available IP Addresses:");

                foreach (IPAddress address in addresses)
                {
                    // Only show IPv4 addresses - they're easier to use for port forwarding
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {address}");
                        System.Diagnostics.Debug.WriteLine($"    Use this address in your port forwarding settings");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Port: {DEFAULT_PORT}");
                System.Diagnostics.Debug.WriteLine("To test if your port is open, visit: https://portchecker.co/");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting network info: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the HTTP listener service
        /// </summary>
        public static void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning)
                    return;

                _cancellationTokenSource?.Cancel();
                _listener?.Close();
                _isRunning = false;

                System.Diagnostics.Debug.WriteLine("HTTP Listener stopped");
            }
        }

        /// <summary>
        /// Processes incoming HTTP requests
        /// </summary>
        private static async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
                {
                    // Get the next request asynchronously
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync();
                        System.Diagnostics.Debug.WriteLine($"Request received from: {context.Request.RemoteEndPoint.Address}:{context.Request.RemoteEndPoint.Port}");
                    }
                    catch (HttpListenerException ex)
                    {
                        if (_isRunning)
                            System.Diagnostics.Debug.WriteLine($"Error getting HTTP context: {ex.Message}");

                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener was stopped
                        break;
                    }

                    // Process the request in a separate task to continue listening
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleRequestAsync(context);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error handling request: {ex.Message}");
                            await SendErrorResponseAsync(context.Response, "Internal server error", 500);
                        }
                    }, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, do nothing
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HTTP listener: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles a single HTTP request
        /// </summary>
        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // Allow CORS for testing purposes
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            // Handle preflight OPTIONS requests for CORS
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            // Log info about the request
            System.Diagnostics.Debug.WriteLine($"Received {request.HttpMethod} request: {request.Url.PathAndQuery}");

            // Only allow POST requests to /process
            if (request.HttpMethod == "POST" && request.Url.AbsolutePath.Equals("/process", StringComparison.OrdinalIgnoreCase))
            {
                await HandleProcessRequestAsync(request, response);
            }
            else
            {
                await SendErrorResponseAsync(response, "Invalid endpoint or method", 404);
            }
        }

        /// <summary>
        /// Handles a document processing request
        /// </summary>
        private static async Task HandleProcessRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Parse the request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                System.Diagnostics.Debug.WriteLine($"Received request body: {requestBody}");

                // Deserialize the request to a command
                CommandListenerService.RemoteCommand command;
                try
                {
                    command = JsonConvert.DeserializeObject<CommandListenerService.RemoteCommand>(requestBody);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deserializing request: {ex.Message}");
                    await SendErrorResponseAsync(response, "Invalid JSON format", 400);
                    return;
                }

                if (command == null || string.IsNullOrEmpty(command.file_name) ||
                    string.IsNullOrEmpty(command.document_type_id) ||
                    string.IsNullOrEmpty(command.transaction_id))
                {
                    await SendErrorResponseAsync(response, "Invalid request format. Required fields: transaction_id, document_type_id, file_name", 400);
                    return;
                }

                // Check if file exists before starting processing
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FileFolder",
                    command.file_name
                );

                if (!File.Exists(filePath))
                {
                    await SendErrorResponseAsync(response, $"File not found: {command.file_name}. Make sure the file exists in the FileFolder directory.", 404);
                    return;
                }

                // Create command and process it
                System.Diagnostics.Debug.WriteLine($"Processing document: {command.file_name}, Type ID: {command.document_type_id}");

                // Log that a command was received via HTTP
                try
                {
                    await ClientLogService.LogActionAsync(ClientActionType.COMMAND_RECEIVED);
                }
                catch (Exception ex)
                {
                    // Log but continue even if logging fails
                    System.Diagnostics.Debug.WriteLine($"Error logging command: {ex.Message}");
                }





                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string _watchFolderPath = Path.Combine(documentsPath, "FileFolder");

                if (!Directory.Exists(_watchFolderPath))
                {
                    Directory.CreateDirectory(_watchFolderPath);
                }

                const string url = "https://si-docflow-server.up.railway.app/document/process?lang=bos&engines=tesseract";

                try
                {
                    using var form = new MultipartFormDataContent();

                    string selectedDocumentName = command.file_name;
                    string selectedDocumentPath = Path.Combine(_watchFolderPath, selectedDocumentName);
                    if (File.Exists(selectedDocumentPath))
                    {
                        string mimeType = GetMimeTypeFromExtension(Path.GetExtension(selectedDocumentPath));
                        var selectedDocumentContent = new ByteArrayContent(File.ReadAllBytes(selectedDocumentPath));
                        selectedDocumentContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                        form.Add(selectedDocumentContent, "file", selectedDocumentName);

                    }
                    form.Add(new StringContent(command.transaction_id), "user");
                    form.Add(new StringContent(Environment.MachineName), "machineId");
                    form.Add(new StringContent(command.document_type_id), "documentTypeId");

                    await ClientLogService.LogActionAsync(ClientActionType.PROCESSING_REQ_SENT);

                    using HttpClient client = new();
                    HttpResponseMessage response2 = await client.PostAsync(url, form);

                    string responseContent = await response2.Content.ReadAsStringAsync();
                    JObject jsonObject;
                    try
                    {
                        jsonObject = JObject.Parse(responseContent);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"The document cannot be processed at the moment: {ex.Message}");
                        return;
                    }

                    if (response2.IsSuccessStatusCode)
                    {
                        var dataPart = jsonObject["data"];
                        if (dataPart == null)
                        {
                            System.Diagnostics.Debug.WriteLine("The server did not return any data");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("Success");


                        // Respond to the client immediately to prevent timeouts
                        var successResponse = new
                        {
                            transaction_id = command.transaction_id,
                            message = "Remote document processing successfully initiated",
                            data = dataPart
                        };

                        await SendJsonResponseAsync(response, successResponse, 200);

                        // Process the command asynchronously after responding to the client
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await DocumentProcessingService.ProcessRemoteCommand(command);
                                System.Diagnostics.Debug.WriteLine($"Document processing completed for: {command.file_name}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in background processing: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failure");
                        return;
                    }
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failure");
                }









                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing document: {ex.Message}");
                await SendErrorResponseAsync(response, $"Error processing document: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Sends a JSON error response
        /// </summary>
        private static async Task SendErrorResponseAsync(HttpListenerResponse response, string message, int statusCode)
        {
            var errorResponse = new
            {
                message = message
            };

            await SendJsonResponseAsync(response, errorResponse, statusCode);
        }

        /// <summary>
        /// Sends a JSON response
        /// </summary>
        private static async Task SendJsonResponseAsync(HttpListenerResponse response, object data, int statusCode)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                var jsonResponse = JsonConvert.SerializeObject(data);
                var buffer = Encoding.UTF8.GetBytes(jsonResponse);

                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending response: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the port for the HTTP listener
        /// </summary>
        public static void SetPort(int port)
        {
            if (_isRunning)
                throw new InvalidOperationException("Cannot change port while the listener is running");

            _urlPrefix = $"http://*:{port}/";
        }

        /// <summary>
        /// Checks if the HTTP listener is running
        /// </summary>
        public static bool IsRunning => _isRunning;


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