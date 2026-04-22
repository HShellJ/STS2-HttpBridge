using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened))]
internal static class CharacterSelectOpenedPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("NCharacterSelectScreen.OnSubmenuOpened postfix fired");
        BridgeSnapshotWriter.SetScreen("CharacterSelect", "CharacterSelectOpened");
    }
}
