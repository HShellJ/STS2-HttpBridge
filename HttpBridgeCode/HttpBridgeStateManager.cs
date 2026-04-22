using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace STS2HttpBridge.HttpBridgeCode;

internal static class HttpBridgeStateManager
{
    private static object? _cachedState;
    private static DateTime _lastUpdateTime = DateTime.MinValue;
    private static readonly object _lock = new();
    private static int _revision = 0;

    public enum RoomType
    {
        None,
        Combat,
        Event,
        Shop,
        RestSite,
        Treasure,
        Map,
        Other
    }

    public static async Task<object> GetCurrentStateAsync()
    {
        lock (_lock)
        {
            // Check if cache is still valid
            var config = HttpBridgeConfig.Load();
            var cacheAge = DateTime.UtcNow - _lastUpdateTime;
            if (_cachedState != null && cacheAge.TotalMilliseconds < config.StateCacheDurationMs)
            {
                return new
                {
                    revision = _revision,
                    cached = true,
                    data = _cachedState
                };
            }
        }

        // Extract fresh state
        var state = await ExtractStateAsync();
        lock (_lock)
        {
            _cachedState = state;
            _lastUpdateTime = DateTime.UtcNow;
            _revision++;
            return new
            {
                revision = _revision,
                cached = false,
                data = state
            };
        }
    }

    private static async Task<object> ExtractStateAsync()
    {
        // Use BridgeStateExtractor to get game state
        // This runs on main thread via dispatcher
        var result = await BridgeMainThreadDispatcher.EnqueueAsync(() =>
        {
            try
            {
                var state = new Dictionary<string, object?>
                {
                    ["timestamp"] = DateTime.UtcNow
                };

                var singleton = BridgeSingleton.Instance;
                if (singleton == null)
                {
                    state["screen"] = "None";
                    return state;
                }

                // Determine current room type
                var roomType = GetCurrentRoomType(singleton);
                state["screen"] = roomType.ToString();

                // Extract state based on current room type
                switch (roomType)
                {
                    case RoomType.Combat:
                        var player = BridgeSingleton.CurrentPlayer;
                        var combatRoom = BridgeSingleton.CurrentCombatRoom;
                        if (player == null || combatRoom == null) break;
                        var combatState = BridgeStateExtractor.ExtractCombat(player, combatRoom);
                        if (combatState != null)
                            state["combat"] = combatState;
                        break;

                    case RoomType.Event:
                        var ev = BridgeSingleton.CurrentEventRoom?.LocalMutableEvent ??
                                 BridgeSingleton.CurrentEventRoom?.CanonicalEvent;
                        if (ev != null)
                        {
                            var eventState = BridgeStateExtractor.ExtractEvent(ev);
                            if (eventState != null)
                                state["event"] = eventState;
                        }
                        break;

                    case RoomType.Shop:
                        var merchantRoom = BridgeSingleton.CurrentMerchantRoom;
                        if (merchantRoom == null) break;
                        var shopState = BridgeStateExtractor.ExtractShop(merchantRoom);
                        if (shopState != null)
                            state["shop"] = shopState;
                        break;

                    case RoomType.RestSite:
                        var restRoom = BridgeSingleton.CurrentRestSiteRoom;
                        if (restRoom == null) break;
                        var restState = BridgeStateExtractor.ExtractRestSite(restRoom);
                        if (restState != null)
                            state["restSite"] = restState;
                        break;

                    case RoomType.Treasure:
                        var treasureRoom = BridgeSingleton.CurrentTreasureRoom;
                        if (treasureRoom == null) break;
                        var treasureState = BridgeStateExtractor.ExtractTreasure(
                            treasureRoom, BridgeSingleton.CurrentTreasureNode);
                        if (treasureState != null)
                            state["treasure"] = treasureState;
                        break;
                }

                // Always include run state if available
                var runState = BridgeStateExtractor.ExtractRun();
                if (runState != null)
                    state["run"] = runState;

                // Include map state if available
                var mapState = BridgeStateExtractor.ExtractMap();
                if (mapState != null)
                    state["map"] = mapState;

                return state;
            }
            catch (Exception ex)
            {
                BridgeTrace.Log($"Error extracting state: {ex.Message}");
                return new Dictionary<string, object?>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };
            }
        });

        return result ?? new Dictionary<string, object?>
        {
            ["error"] = "Failed to extract state",
            ["timestamp"] = DateTime.UtcNow
        };
    }

    private static RoomType GetCurrentRoomType(BridgeSingleton singleton)
    {
        if (BridgeSingleton.CurrentCombatRoom != null)
            return RoomType.Combat;
        if (BridgeSingleton.CurrentEventRoom != null)
            return RoomType.Event;
        if (BridgeSingleton.CurrentMerchantRoom != null)
            return RoomType.Shop;
        if (BridgeSingleton.CurrentRestSiteRoom != null)
            return RoomType.RestSite;
        if (BridgeSingleton.CurrentTreasureRoom != null)
            return RoomType.Treasure;

        // Check if we're on map screen
        try
        {
            // Use reflection or check for map screen instance
            // For now, return Other if none of the above
            return RoomType.Other;
        }
        catch
        {
            return RoomType.None;
        }
    }

    public static bool IsGameRunning()
    {
        // Check if game is in a run
        try
        {
            var singleton = BridgeSingleton.Instance;
            return singleton != null && GetCurrentRoomType(singleton) != RoomType.None;
        }
        catch
        {
            return false;
        }
    }

    public static void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedState = null;
            _revision++;
        }
    }

    public static int GetCurrentRevision()
    {
        lock (_lock)
        {
            return _revision;
        }
    }
}