using System.Runtime.CompilerServices;
using StanceOverhaulFika.Core;

namespace StanceOverhaulFika.Sender
{
    /// <summary>
    /// The only class that touches Stance Overhaul types. Stance Overhaul is a soft dependency: on a
    /// machine without it (headless, or a player who just wants to see others' stances) this class is
    /// never entered, so the assembly is never loaded.
    /// </summary>
    internal static class StanceSource
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool TryRead(out byte stance, out byte flags, out Pose6 pose)
        {
            stance = 0;
            flags = 0;
            pose = default;

            StanceOverhaul.Controllers.StanceController ctrl = StanceOverhaul.Plugin.StanceControllerInstance;
            if (ctrl == null || !ctrl.AwakeRan)
                return false;

            StanceOverhaul.Enums.EStanceType type = ctrl.CurrentStanceType;
            UnityEngine.Vector3 p = ctrl.StancePosition;
            UnityEngine.Vector3 r = ctrl.StanceRotation;

            stance = (byte)type;
            if (type != StanceOverhaul.Enums.EStanceType.None)
                flags |= StanceFlags.Active;
            if (ctrl.IsDoingTacSprint)
                flags |= StanceFlags.TacSprint;

            pose = new Pose6(p.x, p.y, p.z, r.x, r.y, r.z);
            return true;
        }
    }
}
