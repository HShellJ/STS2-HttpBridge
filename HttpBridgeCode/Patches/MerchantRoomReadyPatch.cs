using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
internal static class MerchantRoomReadyPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        BridgeTrace.Log("NMerchantRoom._Ready postfix fired");
        BridgeSnapshotWriter.SetScreen("Shop", "MerchantRoomReady");
        BridgeSingleton.PushCurrentShop("MerchantRoomReady");
    }
}
