using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2HttpBridge.HttpBridgeCode.Patches;

[HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen._Ready))]
internal static class RewardsScreenReadyPatch
{
    public static void Postfix()
    {
        BridgeTrace.Log("NRewardsScreen._Ready postfix fired");
        BridgeSnapshotWriter.SetScreen("Rewards", "RewardsScreenReady");
    }
}
