using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace STS2HttpBridge.HttpBridgeCode;

internal static class HttpBridgeCommandProcessor
{
    public static async Task<CommandResult> ProcessCommandAsync(CommandRequest request)
    {
        try
        {
            // Dispatch command to game thread
            var (status, message) = await BridgeMainThreadDispatcher.EnqueueAsync(() =>
            {
                try
                {
                    // Call BridgeCommandDispatcher with JsonElement
                    return BridgeCommandDispatcher.Dispatch(request.Type, request.Command);
                }
                catch (Exception ex)
                {
                    BridgeTrace.Log($"Error dispatching command: {ex.Message}");
                    return ("error", $"Dispatch threw: {ex.Message}");
                }
            });

            // Convert result to CommandResult
            return new CommandResult
            {
                Status = status,
                Message = message,
                Revision = HttpBridgeStateManager.GetCurrentRevision(),
                TimestampUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            BridgeTrace.Log($"Error processing command: {ex.Message}");
            return new CommandResult
            {
                Status = "error",
                Message = ex.Message,
                Revision = HttpBridgeStateManager.GetCurrentRevision(),
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}