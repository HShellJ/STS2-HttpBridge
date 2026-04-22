using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NSingleplayerSubmenu), nameof(NSingleplayerSubmenu.OnSubmenuOpened))]
internal static class SingleplayerSubmenuOpenedPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("NSingleplayerSubmenu.OnSubmenuOpened postfix fired");
        BridgeSnapshotWriter.SetScreen("SingleplayerSubmenu", "SingleplayerSubmenuOpened");
    }
}
