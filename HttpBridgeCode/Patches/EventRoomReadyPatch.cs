using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NEventRoom), "_Ready")]
internal static class EventRoomReadyPatch
{
    public static void Postfix(NEventRoom __instance)
    {
        BridgeTrace.Log("NEventRoom._Ready postfix fired");
        BridgeSnapshotWriter.SetScreen("Event", "EventRoomReady");
        BridgeSingleton.PushCurrentEvent("EventRoomReady");
    }
}
