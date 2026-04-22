using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NSingleplayerSubmenu), nameof(NSingleplayerSubmenu._Ready))]
internal static class SingleplayerSubmenuReadyPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("SingleplayerSubmenu _Ready postfix fired");
        BridgeSnapshotWriter.SetScreen("SingleplayerSubmenu", "SingleplayerSubmenuReady");
    }
}
