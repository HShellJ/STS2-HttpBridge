using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen._Ready))]
internal static class MapScreenReadyPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("MapScreen _Ready postfix fired");
        BridgeSnapshotWriter.SetScreen("Map", "MapScreenReady");
    }
}
