using docflow.Models;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WIA;
using Windows.Devices.Enumeration;
using System.Text.Json;

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
        private static int DEFAULT_PORT = AppSettings.PORT;
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
                        System.Diagnostics.Debug.WriteLine("Access denied for *:8080");
                        throw;
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
                bool boolResult = await StartScan(command.file_name);
                if(boolResult == false)
                {
                    await SendErrorResponseAsync(response, "Scan device not found!", 404);
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

                // Respond to the client immediately to prevent timeouts
                var successResponse = new
                {
                    transaction_id = command.transaction_id,
                    message = "Remote document processing successfully initiated"
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing document: {ex.Message}");
                await SendErrorResponseAsync(response, $"Error processing document: {ex.Message}", 500);
            }
        }
        private static async Task<bool> StartScan(string documentName)
        {
            bool hasOpenCameraFailed = false;

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(folderPath, "docflow");
            string path = Path.Combine(appFolder, "DevicesWindow.json");
            if (!File.Exists(path))
            {
                return false;
            }

            string jsonString = File.ReadAllText(path);
            var savedDevice = System.Text.Json.JsonSerializer.Deserialize<InfoDev>(jsonString);
            if (savedDevice == null || string.IsNullOrEmpty(savedDevice.Name))
            {
                return false;
            }
            if (savedDevice.Device == DeviceTYPE.Camera)
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        string targetName = savedDevice?.Name;
                        if (string.IsNullOrEmpty(targetName))
                        {
                            hasOpenCameraFailed = true;
                            return;
                        }
                        var allDevices = DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture).AsTask().Result;
                        int targetIndex = -1;

                        for (int i = 0; i < allDevices.Count; i++)
                        {
                            if (allDevices[i].Name == targetName)
                            {
                                targetIndex = i;
                                break;
                            }
                        }

                        if (targetIndex == -1)
                        {
                            hasOpenCameraFailed = true;

                            return;
                        }
                        using var capture = new VideoCapture(targetIndex);
                        if (capture.IsOpened())
                        {
                            using var window = new OpenCvSharp.Window("Press SPACE to take photo, ESC to cancel.");
                            using var frame = new Mat();
                            for (int i = 0; i < 20; i++)
                            {
                                capture.Read(frame);
                                Cv2.WaitKey(30);
                            }
                            while (true)
                            {
                                capture.Read(frame);
                                capture.Read(frame);
                                if (!frame.Empty())
                                {
                                    string documentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileFolder", documentName);
                                    Cv2.ImWrite(documentPath, frame);
                                    window.ShowImage(frame); 
                                    break;
                                }
                            }

                        }
                        else
                        {
                            hasOpenCameraFailed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        hasOpenCameraFailed = true;
                    }
                });
            }
            else if (savedDevice.Device == DeviceTYPE.Scanner)
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to scan with: {savedDevice.Name} (ID: {savedDevice.Id})");
                await Task.Run(async () =>
                {
                    try
                    {
                        string scannerDeviceId = savedDevice.Id;

                        var deviceManager = new DeviceManager();
                        Device scanner = null;

                        foreach (WIA.DeviceInfo info in deviceManager.DeviceInfos)
                        {
                            if (info.Type == WiaDeviceType.ScannerDeviceType && info.DeviceID == scannerDeviceId)
                            {
                                scanner = info.Connect();
                                break;
                            }
                        }

                        if (scanner == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Scanner with ID {scannerDeviceId} not found.");
                            hasOpenCameraFailed = true;
                            return;
                        }

                        Item scanItem = scanner.Items[1];


                        Action<IProperties, object, object> SetWIAPropertyLocal = (properties, propName, propValue) =>
                        {
                            try
                            {
                                foreach (Property prop in properties)
                                {
                                    if (prop.Name == propName.ToString() || prop.PropertyID == Convert.ToInt32(propName))
                                    {
                                        prop.set_Value(propValue);
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error setting WIA property {propName}: {ex.Message}");
                            }
                        };


                        SetWIAPropertyLocal(scanItem.Properties, "6146", 1);  // WIA_IPA_ITEM_FLAG (1 = Flatbed or Feeder)
                        SetWIAPropertyLocal(scanItem.Properties, "6147", 1);  // WIA_IPA_ACCESS_RIGHTS (1 = Read)
                        SetWIAPropertyLocal(scanItem.Properties, "4104", 4);  // WIA_IPA_DEPTH (4 = Color, 2 = Grayscale, 1 = Black and White)
                        SetWIAPropertyLocal(scanItem.Properties, "6149", 300); // WIA_IPA_DPI_X (Horizontal Resolution - npr. 300 DPI)
                        SetWIAPropertyLocal(scanItem.Properties, "6150", 300); // WIA_IPA_DPI_Y (Vertical Resolution - npr. 300 DPI)
                        SetWIAPropertyLocal(scanItem.Properties, "6154", 0);   // WIA_IPA_XPOS (X-Offset)
                        SetWIAPropertyLocal(scanItem.Properties, "6155", 0);   // WIA_IPA_YPOS (Y-Offset)




                        object image = scanItem.Transfer("{B96B3CA6-0728-11D3-9EB1-00C04F72D991}");

                        var imageFile = (ImageFile)image;

                        string fullFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileFolder", documentName);

                        imageFile.SaveFile(fullFilePath);

                        System.Diagnostics.Debug.WriteLine($"Document scanned and saved to: {fullFilePath}");

                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"WIA Scan Error: {ex.Message} (HRESULT: {ex.ErrorCode})");
                        hasOpenCameraFailed = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"General Scan Error: {ex.Message}");
                        hasOpenCameraFailed = true;
                    }
                });
            }
            if (hasOpenCameraFailed)
            {
                return false;
            }
            return true;
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
    }
}