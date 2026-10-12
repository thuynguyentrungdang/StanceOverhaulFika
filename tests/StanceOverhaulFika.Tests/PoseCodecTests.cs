using StanceOverhaulFika.Core;
using Xunit;

namespace StanceOverhaulFika.Tests
{
    public class PoseCodecTests
    {
        [Theory]
        [InlineData(0f)]
        [InlineData(0.05f)]
        [InlineData(-0.123f)]
        [InlineData(1.5f)]
        [InlineData(-3.2f)]
        public void Position_round_trips_within_resolution(float v)
        {
            float back = PoseCodec.DecodePos(PoseCodec.EncodePos(v));
            Assert.InRange(back - v, -1f / PoseCodec.PosScale, 1f / PoseCodec.PosScale);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(12.34f)]
        [InlineData(-90f)]
        [InlineData(180f)]
        [InlineData(-300f)]
        public void Rotation_round_trips_within_resolution(float v)
        {
            float back = PoseCodec.DecodeRot(PoseCodec.EncodeRot(v));
            Assert.InRange(back - v, -1f / PoseCodec.RotScale, 1f / PoseCodec.RotScale);
        }

        [Fact]
        public void Out_of_range_values_clamp_instead_of_wrapping()
        {
            Assert.Equal(short.MaxValue, PoseCodec.EncodePos(100f));
            Assert.Equal(short.MinValue, PoseCodec.EncodePos(-100f));
            Assert.Equal(short.MaxValue, PoseCodec.EncodeRot(10000f));
            Assert.Equal(short.MinValue, PoseCodec.EncodeRot(-10000f));
        }

        [Fact]
        public void Nan_and_infinity_become_zero()
        {
            Assert.Equal(0, PoseCodec.EncodePos(float.NaN));
            Assert.Equal(0, PoseCodec.EncodePos(float.PositiveInfinity));
            Assert.Equal(0, PoseCodec.EncodeRot(float.NegativeInfinity));
        }

        [Fact]
        public void QPose_round_trip_keeps_stance_flags_and_pose()
        {
            var p = new Pose6(0.04f, -0.02f, 0.11f, 12.5f, -7.25f, 3f);
            QPose q = QPose.From(2, StanceFlags.Active | StanceFlags.TacSprint, p);
            Pose6 back = q.ToPose();

            Assert.Equal(2, q.Stance);
            Assert.Equal(StanceFlags.Active | StanceFlags.TacSprint, q.Flags);
            Assert.InRange(back.PX - p.PX, -0.0001f, 0.0001f);
            Assert.InRange(back.PY - p.PY, -0.0001f, 0.0001f);
            Assert.InRange(back.PZ - p.PZ, -0.0001f, 0.0001f);
            Assert.InRange(back.RX - p.RX, -0.01f, 0.01f);
            Assert.InRange(back.RY - p.RY, -0.01f, 0.01f);
            Assert.InRange(back.RZ - p.RZ, -0.01f, 0.01f);
        }

        [Fact]
        public void QPose_equality_covers_every_field()
        {
            var a = new QPose(1, 2, 3, 4, 5, 6, 7, 8);
            Assert.True(a.Equals(new QPose(1, 2, 3, 4, 5, 6, 7, 8)));
            Assert.False(a.Equals(new QPose(9, 2, 3, 4, 5, 6, 7, 8)));
            Assert.False(a.Equals(new QPose(1, 9, 3, 4, 5, 6, 7, 8)));
            Assert.False(a.Equals(new QPose(1, 2, 9, 4, 5, 6, 7, 8)));
            Assert.False(a.Equals(new QPose(1, 2, 3, 4, 5, 6, 7, 9)));
        }
    }

    public class SeqUtilTests
    {
        [Fact]
        public void Newer_older_and_equal()
        {
            Assert.True(SeqUtil.IsNewer(5, 4));
            Assert.False(SeqUtil.IsNewer(4, 5));
            Assert.False(SeqUtil.IsNewer(5, 5));
        }

        [Fact]
        public void Wrap_around_counts_as_newer()
        {
            Assert.True(SeqUtil.IsNewer(0, ushort.MaxValue));
            Assert.True(SeqUtil.IsNewer(3, 65530));
            Assert.False(SeqUtil.IsNewer(65530, 3));
        }
    }
}
