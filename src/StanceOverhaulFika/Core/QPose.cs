using System;

namespace StanceOverhaulFika.Core
{
    /// <summary>Quantised stance pose exactly as it travels on the wire.</summary>
    public readonly struct QPose : IEquatable<QPose>
    {
        public readonly byte Stance;
        public readonly byte Flags;
        public readonly short PX, PY, PZ, RX, RY, RZ;

        public QPose(byte stance, byte flags, short px, short py, short pz, short rx, short ry, short rz)
        {
            Stance = stance; Flags = flags;
            PX = px; PY = py; PZ = pz;
            RX = rx; RY = ry; RZ = rz;
        }

        public static QPose From(byte stance, byte flags, in Pose6 p)
        {
            return new QPose(stance, flags,
                PoseCodec.EncodePos(p.PX), PoseCodec.EncodePos(p.PY), PoseCodec.EncodePos(p.PZ),
                PoseCodec.EncodeRot(p.RX), PoseCodec.EncodeRot(p.RY), PoseCodec.EncodeRot(p.RZ));
        }

        public Pose6 ToPose()
        {
            return new Pose6(
                PoseCodec.DecodePos(PX), PoseCodec.DecodePos(PY), PoseCodec.DecodePos(PZ),
                PoseCodec.DecodeRot(RX), PoseCodec.DecodeRot(RY), PoseCodec.DecodeRot(RZ));
        }

        public bool Equals(QPose o)
        {
            return Stance == o.Stance && Flags == o.Flags
                && PX == o.PX && PY == o.PY && PZ == o.PZ
                && RX == o.RX && RY == o.RY && RZ == o.RZ;
        }

        public override bool Equals(object obj) => obj is QPose o && Equals(o);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Stance;
                h = (h * 31) + Flags;
                h = (h * 31) + PX; h = (h * 31) + PY; h = (h * 31) + PZ;
                h = (h * 31) + RX; h = (h * 31) + RY; h = (h * 31) + RZ;
                return h;
            }
        }

        public static bool operator ==(QPose a, QPose b) => a.Equals(b);
        public static bool operator !=(QPose a, QPose b) => !a.Equals(b);
    }
}
