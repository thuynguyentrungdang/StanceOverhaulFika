using EFT.Animations;
using HarmonyLib;
using StanceOverhaulFika.Core;
using StanceOverhaulFika.Receiver;
using UnityEngine;

namespace StanceOverhaulFika.Patches
{
    /// <summary>
    /// Adds a remote player's stance to the hands springs' rest position.
    ///
    /// Third-person PWA strategy runs ZeroAdjustments -> ApplyPosition (HandsPosition.Get) ->
    /// ApplySimpleRotation (HandsRotation.Get) inside ObservedPlayer.ObservedVisualPass, and Fika copies
    /// WeaponRootAnim into the skeleton right after. The springs ease toward Zero, so the remote gun
    /// moves the same way the local one does under Stance Overhaul.
    ///
    /// Only PWAs registered in <see cref="RemoteStances"/> are touched; the local player (Stance
    /// Overhaul's own clone springs) and everything else pass straight through.
    /// </summary>
    [HarmonyPatch(typeof(ProceduralWeaponAnimation), nameof(ProceduralWeaponAnimation.ZeroAdjustments))]
    internal static class ZeroAdjustmentsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
        {
            if (!RemoteStances.TryGetByPwa(__instance, out RemoteStance state))
                return;

            var hands = __instance.HandsContainer;
            if (hands == null)
                return;

            Pose6 p = state.Smoother.Applied;
            hands.HandsPosition.Zero += new Vector3(p.PX, p.PY, p.PZ);
            hands.HandsRotation.Zero += new Vector3(p.RX, p.RY, p.RZ);
        }
    }
}
