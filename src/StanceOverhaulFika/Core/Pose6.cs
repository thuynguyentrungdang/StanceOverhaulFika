namespace StanceOverhaulFika.Core
{
    /// <summary>
    /// Stance offset as plain floats: position (metres) + euler rotation (degrees).
    /// No Unity types so the math in Core stays unit-testable.
    /// </summary>
    public readonly struct Pose6
    {
        public readonly float PX, PY, PZ, RX, RY, RZ;

        public Pose6(float px, float py, float pz, float rx, float ry, float rz)
        {
            PX = px; PY = py; PZ = pz;
            RX = rx; RY = ry; RZ = rz;
        }

        public static Pose6 Lerp(in Pose6 a, in Pose6 b, float t)
        {
            return new Pose6(
                a.PX + (b.PX - a.PX) * t, a.PY + (b.PY - a.PY) * t, a.PZ + (b.PZ - a.PZ) * t,
                a.RX + (b.RX - a.RX) * t, a.RY + (b.RY - a.RY) * t, a.RZ + (b.RZ - a.RZ) * t);
        }

        public static Pose6 Scale(in Pose6 a, float s)
        {
            return new Pose6(a.PX * s, a.PY * s, a.PZ * s, a.RX * s, a.RY * s, a.RZ * s);
        }
    }
}
