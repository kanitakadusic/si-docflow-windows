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
        private const int DEFAULT_PORT = 80;
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
                    _cancellationTokenSource = new CancellationTokenSource();
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(_urlPrefix);
                    _listener.Start();
                    _isRunning = true;

                    System.Diagnostics.Debug.WriteLine($"HTTP Listener started on {_urlPrefix}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start HTTP Listener: {ex.Message}");
                    throw;
                }
            }

            // Start processing requests
            await ProcessRequestsAsync(_cancellationTokenSource.Token);
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

                // Deserialize the request to a command
                var command = JsonConvert.DeserializeObject<CommandListenerService.RemoteCommand>(requestBody);

                if (command == null || string.IsNullOrEmpty(command.file_name) ||
                    string.IsNullOrEmpty(command.document_type_id) ||
                    string.IsNullOrEmpty(command.transaction_id))
                {
                    await SendErrorResponseAsync(response, "Invalid request format. Required fields: transaction_id, document_type_id, file_name", 400);
                    return;
                }

                // Create command and process it
                System.Diagnostics.Debug.WriteLine($"Processing document: {command.file_name}, Type ID: {command.document_type_id}");

                // Log that a command was received via HTTP
                await ClientLogService.LogActionAsync(ClientActionType.COMMAND_RECEIVED);

                // Process the command
                await DocumentProcessingService.ProcessRemoteCommand(command);

                // Respond to the client
                var successResponse = new
                {
                    success = true,
                    message = "Document processing initiated successfully",
                    transaction_id = command.transaction_id
                };

                await SendJsonResponseAsync(response, successResponse, 200);
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
                success = false,
                message = message
            };

            await SendJsonResponseAsync(response, errorResponse, statusCode);
        }

        /// <summary>
        /// Sends a JSON response
        /// </summary>
        private static async Task SendJsonResponseAsync(HttpListenerResponse response, object data, int statusCode)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            var jsonResponse = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(jsonResponse);

            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
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
    }
}