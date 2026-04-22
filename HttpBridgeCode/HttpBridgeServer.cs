using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace STS2HttpBridge.HttpBridgeCode;

internal static class HttpBridgeServer
{
    private static HttpListener? _listener;
    private static Thread? _serverThread;
    private static volatile bool _isRunning;
    private static readonly object _lock = new();
    private static HttpBridgeConfig _config = new();

    public static void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            MainFile.Logger.Info("HttpBridgeServer.Start() called");

            // Load configuration
            _config = HttpBridgeConfig.Load();
            MainFile.Logger.Info($"Config loaded: Host={_config.Host}, Port={_config.Port}");

            try
            {
                _listener = new HttpListener();
                string prefix = $"http://{_config.Host}:{_config.Port}/";
                MainFile.Logger.Info($"Adding prefix: {prefix}");
                _listener.Prefixes.Add(prefix);

                MainFile.Logger.Info("Calling HttpListener.Start()...");
                _listener.Start();
                MainFile.Logger.Info("HttpListener.Start() OK");

                _isRunning = true;
                _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "HttpBridgeServer" };
                _serverThread.Start();
                MainFile.Logger.Info("Server thread started");

                BridgeTrace.Log($"HTTP server started on {prefix}");
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Failed to start HTTP server: {ex}");
                BridgeTrace.Log($"Failed to start HTTP server: {ex.Message}");
                _isRunning = false;
            }
        }
    }

    public static void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;
            try
            {
                _listener?.Stop();
                _listener?.Close();
                _listener = null;

                _serverThread?.Join(2000);
                _serverThread = null;

                BridgeTrace.Log("HTTP server stopped");
            }
            catch (Exception ex)
            {
                BridgeTrace.Log($"Error stopping HTTP server: {ex.Message}");
            }
        }
    }

    private static void ServerLoop()
    {
        while (_isRunning && _listener != null)
        {
            try
            {
                // GetContextAsync is blocking, but we need to check _isRunning periodically
                var context = _listener.GetContext();
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch (HttpListenerException) when (!_isRunning)
            {
                // Listener stopped, exit loop
                break;
            }
            catch (Exception ex)
            {
                BridgeTrace.Log($"Error in HTTP server loop: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }

    private static async Task ProcessRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // Handle CORS preflight
            if (request.HttpMethod == "OPTIONS")
            {
                HandleCorsPreflight(response);
                return;
            }

            // Apply CORS headers
            if (_config.EnableCors)
            {
                response.Headers.Add("Access-Control-Allow-Origin", _config.AllowedOrigins);
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-API-Key");
            }

            // Route request
            string path = request.Url?.AbsolutePath ?? "";
            switch (path)
            {
                case "/api/state":
                    if (request.HttpMethod == "GET")
                        await HttpBridgeController.HandleGetState(context);
                    else
                        WriteResponse(response, 405, "Method Not Allowed");
                    break;

                case "/api/command":
                    if (request.HttpMethod == "POST")
                        await HttpBridgeController.HandlePostCommand(context);
                    else
                        WriteResponse(response, 405, "Method Not Allowed");
                    break;

                case "/api/health":
                    if (request.HttpMethod == "GET")
                        await HttpBridgeController.HandleHealthCheck(context);
                    else
                        WriteResponse(response, 405, "Method Not Allowed");
                    break;

                case "/api/config":
                    if (request.HttpMethod == "GET")
                        await HttpBridgeController.HandleGetConfig(context);
                    else if (request.HttpMethod == "POST")
                        await HttpBridgeController.HandlePostConfig(context);
                    else
                        WriteResponse(response, 405, "Method Not Allowed");
                    break;

                default:
                    WriteResponse(response, 404, "Not Found");
                    break;
            }
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error processing HTTP request: {ex.Message}");
            try
            {
                WriteResponse(context.Response, 500, "Internal Server Error");
            }
            catch { }
        }
    }

    private static void HandleCorsPreflight(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        if (_config.EnableCors)
        {
            response.Headers.Add("Access-Control-Allow-Origin", _config.AllowedOrigins);
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-API-Key");
            response.Headers.Add("Access-Control-Max-Age", "86400");
        }
        response.Close();
    }

    private static void WriteResponse(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/plain";
        using (var writer = new StreamWriter(response.OutputStream))
        {
            writer.Write(message);
        }
        response.Close();
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static async Task WriteJsonResponse(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        string json = JsonSerializer.Serialize(data, JsonOptions);
        using (var writer = new StreamWriter(response.OutputStream))
        {
            await writer.WriteAsync(json);
        }
        response.Close();
    }

    internal static async Task<T?> ReadJsonRequest<T>(HttpListenerRequest request)
    {
        using (var reader = new StreamReader(request.InputStream))
        {
            string json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
    }
}