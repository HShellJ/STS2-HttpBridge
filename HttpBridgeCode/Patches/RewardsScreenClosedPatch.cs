using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.AfterOverlayClosed))]
internal static class RewardsScreenClosedPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("NRewardsScreen.AfterOverlayClosed postfix fired");
        BridgeSnapshotWriter.SetRewards(null, "RewardsScreenClosed-ClearPayload");
        BridgeSnapshotWriter.SetScreen("RewardsClosed", "RewardsScreenClosed");
    }
}
