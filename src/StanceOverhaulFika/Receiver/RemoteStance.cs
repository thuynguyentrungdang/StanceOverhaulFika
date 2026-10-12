using EFT.Animations;
using StanceOverhaulFika.Core;
using StanceOverhaulFika.Net;

namespace StanceOverhaulFika.Receiver
{
    /// <summary>What we currently know about one remote player's stance.</summary>
    internal sealed class RemoteStance
    {
        internal readonly int NetId;
        internal readonly PoseSmoother Smoother = new PoseSmoother();

        internal ProceduralWeaponAnimation Pwa;
        internal byte Stance;
        internal byte Flags;
        internal float LastReceived;

        private ushort _lastSeq;
        private bool _hasSeq;

        internal RemoteStance(int netId) => NetId = netId;

        /// <summary>Takes a packet if it is newer than the last one. Returns false for stale/duplicate packets.</summary>
        internal bool Accept(in StancePosePacket packet, float now)
        {
            if (_hasSeq && !SeqUtil.IsNewer(packet.Seq, _lastSeq))
                return false;

            Pose6 pose = packet.Pose.ToPose();
            if (!_hasSeq)
                Smoother.Snap(in pose); // first sight of this player: no swing in from zero
            else
                Smoother.SetTarget(in pose);

            _hasSeq = true;
            _lastSeq = packet.Seq;
            Stance = packet.Pose.Stance;
            Flags = packet.Pose.Flags;
            LastReceived = now;
            return true;
        }
    }
}
