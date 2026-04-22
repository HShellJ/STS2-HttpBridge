using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NEventRoom), "Proceed")]
internal static class EventRoomProceedPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("NEventRoom.Proceed postfix fired");
        BridgeSnapshotWriter.SetEvent(null, "EventProceed-ClearPayload");
    }
}
