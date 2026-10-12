using StanceOverhaulFika.Core;
using Xunit;

namespace StanceOverhaulFika.Tests
{
    public class PoseSmootherTests
    {
        [Fact]
        public void Factor_is_zero_for_no_time_or_no_speed()
        {
            Assert.Equal(0f, PoseSmoother.Factor(0f, 25f));
            Assert.Equal(0f, PoseSmoother.Factor(0.016f, 0f));
        }

        [Fact]
        public void Factor_is_frame_rate_independent()
        {
            // one 100 ms step must equal ten 10 ms steps
            float oneStep = PoseSmoother.Factor(0.1f, 20f);
            float remaining = 1f;
            for (int i = 0; i < 10; i++)
                remaining *= 1f - PoseSmoother.Factor(0.01f, 20f);
            Assert.InRange((1f - remaining) - oneStep, -0.0001f, 0.0001f);
        }

        [Fact]
        public void Pose_converges_to_target()
        {
            var s = new PoseSmoother();
            s.SetTarget(new Pose6(0.1f, 0, 0, 30f, 0, 0));
            for (int i = 0; i < 120; i++)
                s.Tick(1f / 60f, 25f, 1f, 10f);
            Assert.InRange(s.Current.PX, 0.0999f, 0.1001f);
            Assert.InRange(s.Current.RX, 29.99f, 30.01f);
        }

        [Fact]
        public void Snap_jumps_without_smoothing()
        {
            var s = new PoseSmoother();
            s.Snap(new Pose6(0.2f, 0, 0, 10f, 0, 0));
            Assert.Equal(0.2f, s.Current.PX);
            Assert.Equal(0.2f, s.Target.PX);
        }

        [Fact]
        public void Applied_pose_is_scaled_by_weight()
        {
            var s = new PoseSmoother();
            s.Snap(new Pose6(0.2f, 0, 0, 10f, 0, 0));
            Assert.Equal(0f, s.Applied.PX);            // weight starts at 0: fades in, no pop

            for (int i = 0; i < 120; i++)
                s.Tick(1f / 60f, 25f, 1f, 10f);
            Assert.InRange(s.Applied.PX, 0.199f, 0.201f);
        }

        [Fact]
        public void Weight_fades_out_when_target_is_zero()
        {
            var s = new PoseSmoother();
            s.Snap(new Pose6(0.2f, 0, 0, 10f, 0, 0));
            for (int i = 0; i < 120; i++)
                s.Tick(1f / 60f, 25f, 1f, 10f);
            for (int i = 0; i < 120; i++)
                s.Tick(1f / 60f, 25f, 0f, 10f);
            Assert.InRange(s.Applied.PX, -0.001f, 0.001f);
        }
    }
}
