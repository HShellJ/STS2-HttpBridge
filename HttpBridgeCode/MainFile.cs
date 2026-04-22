using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace STS2HttpBridge.HttpBridgeCode;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "STS2HttpBridge";
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static Harmony? _harmony;

    public static void Initialize()
    {
        Logger.Info("HttpBridge Initialize() called");

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            Logger.Info($"Assembly: {assembly.FullName}");
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
            Logger.Info("LookupScriptsInAssembly OK");

            _harmony ??= new Harmony(ModId);
            _harmony.PatchAll(assembly);
            Logger.Info("Harmony PatchAll OK");

            BridgeTrace.Log("Initialize start");

            // Install the main-thread dispatcher node so the background command reader
            // can marshal game-API calls back onto the engine main thread.
            BridgeMainThreadDispatcher.EnsureInstalled();

            // Start HTTP server
            HttpBridgeServer.Start();

            // Register shutdown hook to stop HTTP server when the game process exits
            AppDomain.CurrentDomain.ProcessExit += (_, _) => HttpBridgeServer.Stop();

            Logger.Info($"HttpBridge initialized. HTTP server should be starting on port 8080.");
        }
        catch (Exception ex)
        {
            Logger.Error($"HttpBridge Initialize failed: {ex}");
        }
    }
}
