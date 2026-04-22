using System;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;

namespace STS2HttpBridge.HttpBridgeCode;

internal static class HttpBridgeController
{
    // GET /api/state
    public static async Task HandleGetState(HttpListenerContext context)
    {
        try
        {
            var state = await HttpBridgeStateManager.GetCurrentStateAsync();
            await HttpBridgeServer.WriteJsonResponse(context.Response, 200, state);
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error getting state: {ex.Message}");
            await HttpBridgeServer.WriteJsonResponse(context.Response, 500, new { error = "Failed to get game state" });
        }
    }

    // POST /api/command
    public static async Task HandlePostCommand(HttpListenerContext context)
    {
        try
        {
            var request = await HttpBridgeServer.ReadJsonRequest<CommandRequest>(context.Request);
            if (request == null)
            {
                await HttpBridgeServer.WriteJsonResponse(context.Response, 400, new { error = "Invalid JSON" });
                return;
            }

            // Validate API key if configured
            var config = HttpBridgeConfig.Load();
            if (!string.IsNullOrEmpty(config.ApiKey) && request.ApiKey != config.ApiKey)
            {
                await HttpBridgeServer.WriteJsonResponse(context.Response, 401, new { error = "Invalid API key" });
                return;
            }

            var result = await HttpBridgeCommandProcessor.ProcessCommandAsync(request);
            await HttpBridgeServer.WriteJsonResponse(context.Response, 200, result);
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error processing command: {ex.Message}");
            await HttpBridgeServer.WriteJsonResponse(context.Response, 500, new { error = "Failed to process command" });
        }
    }

    // GET /api/health
    public static async Task HandleHealthCheck(HttpListenerContext context)
    {
        try
        {
            var health = new
            {
                status = "healthy",
                serverTime = DateTime.UtcNow,
                gameRunning = HttpBridgeStateManager.IsGameRunning()
            };
            await HttpBridgeServer.WriteJsonResponse(context.Response, 200, health);
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error in health check: {ex.Message}");
            await HttpBridgeServer.WriteJsonResponse(context.Response, 500, new { error = "Health check failed" });
        }
    }

    // GET /api/config
    public static async Task HandleGetConfig(HttpListenerContext context)
    {
        try
        {
            var config = HttpBridgeConfig.Load();
            await HttpBridgeServer.WriteJsonResponse(context.Response, 200, config);
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error getting config: {ex.Message}");
            await HttpBridgeServer.WriteJsonResponse(context.Response, 500, new { error = "Failed to get config" });
        }
    }

    // POST /api/config
    public static async Task HandlePostConfig(HttpListenerContext context)
    {
        try
        {
            var newConfig = await HttpBridgeServer.ReadJsonRequest<HttpBridgeConfig>(context.Request);
            if (newConfig == null)
            {
                await HttpBridgeServer.WriteJsonResponse(context.Response, 400, new { error = "Invalid JSON" });
                return;
            }

            // Validate port range
            if (newConfig.Port < 1 || newConfig.Port > 65535)
            {
                await HttpBridgeServer.WriteJsonResponse(context.Response, 400, new { error = "Port must be between 1 and 65535" });
                return;
            }

            HttpBridgeConfig.Save(newConfig);
            await HttpBridgeServer.WriteJsonResponse(context.Response, 200, new { message = "Configuration updated" });
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error updating config: {ex.Message}");
            await HttpBridgeServer.WriteJsonResponse(context.Response, 500, new { error = "Failed to update config" });
        }
    }
}

// Request/response models
internal class CommandRequest
{
    public string ApiKey { get; set; } = "";
    public string Type { get; set; } = "";
    public JsonElement Command { get; set; }
}

internal class CommandResult
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public int Revision { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}