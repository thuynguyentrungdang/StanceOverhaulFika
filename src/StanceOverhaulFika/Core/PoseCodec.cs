using System;

namespace StanceOverhaulFika.Core
{
    /// <summary>Wire quantisation. Position 0.1 mm (range +-3.27 m), rotation 0.01 deg (range +-327 deg).</summary>
    public static class PoseCodec
    {
        public const float PosScale = 10000f;
        public const float RotScale = 100f;

        public static short EncodePos(float v) => Encode(v, PosScale);
        public static short EncodeRot(float v) => Encode(v, RotScale);
        public static float DecodePos(short q) => q / PosScale;
        public static float DecodeRot(short q) => q / RotScale;

        private static short Encode(float v, float scale)
        {
            if (float.IsNaN(v) || float.IsInfinity(v))
                return 0;
            double s = Math.Round((double)v * scale);
            if (s > short.MaxValue) s = short.MaxValue;
            else if (s < short.MinValue) s = short.MinValue;
            return (short)s;
        }
    }

    public static class SeqUtil
    {
        /// <summary>True when <paramref name="a"/> is ahead of <paramref name="b"/> (ushort wrap-around safe).</summary>
        public static bool IsNewer(ushort a, ushort b)
        {
            ushort diff = unchecked((ushort)(a - b));
            return diff != 0 && diff < 0x8000;
        }
    }

    public static class StanceFlags
    {
        public const byte TacSprint = 1 << 0;
        public const byte Active = 1 << 1;
    }
}
