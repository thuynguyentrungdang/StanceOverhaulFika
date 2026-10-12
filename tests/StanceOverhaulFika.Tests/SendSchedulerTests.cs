using StanceOverhaulFika.Core;
using Xunit;

namespace StanceOverhaulFika.Tests
{
    public class SendSchedulerTests
    {
        private const float Interval = 0.05f;   // 20 Hz
        private const float Settle = 0.3f;
        private const float Keepalive = 1f;

        private static SendScheduler Make() => new SendScheduler(Interval, Settle, Keepalive);
        private static QPose Pose(byte stance = 1, byte flags = 2, short px = 0) => new QPose(stance, flags, px, 0, 0, 0, 0, 0);

        [Fact]
        public void First_sample_is_sent()
        {
            Assert.Equal(SendKind.Unreliable, Make().Update(0f, Pose()));
        }

        [Fact]
        public void Unchanged_pose_is_quiet_until_settle()
        {
            var s = Make();
            s.Update(0f, Pose());
            Assert.Equal(SendKind.None, s.Update(0.1f, Pose()));
            Assert.Equal(SendKind.None, s.Update(0.29f, Pose()));
        }

        [Fact]
        public void Settled_pose_sends_exactly_one_reliable()
        {
            var s = Make();
            s.Update(0f, Pose());
            Assert.Equal(SendKind.Reliable, s.Update(0.31f, Pose()));
            Assert.Equal(SendKind.None, s.Update(0.32f, Pose()));
            Assert.Equal(SendKind.None, s.Update(0.9f, Pose()));
        }

        [Fact]
        public void Keepalive_fires_after_the_interval_and_repeats()
        {
            var s = Make();
            s.Update(0f, Pose());
            s.Update(0.31f, Pose());                                        // reliable settle at 0.31
            Assert.Equal(SendKind.None, s.Update(1.0f, Pose()));            // 0.69 s since last send
            Assert.Equal(SendKind.Unreliable, s.Update(1.32f, Pose()));     // >= 1 s since last send
            Assert.Equal(SendKind.None, s.Update(1.5f, Pose()));
            Assert.Equal(SendKind.Unreliable, s.Update(2.33f, Pose()));
        }

        [Fact]
        public void Pose_change_within_interval_is_held_back_then_sent()
        {
            var s = Make();
            s.Update(0f, Pose(px: 0));
            Assert.Equal(SendKind.None, s.Update(0.02f, Pose(px: 5)));        // rate limited
            Assert.Equal(SendKind.None, s.Update(0.04f, Pose(px: 6)));
            Assert.Equal(SendKind.Unreliable, s.Update(0.06f, Pose(px: 7))); // latest value goes out
        }

        [Fact]
        public void Held_back_pose_still_goes_out_if_motion_stops_mid_interval()
        {
            var s = Make();
            s.Update(0f, Pose(px: 0));
            Assert.Equal(SendKind.None, s.Update(0.02f, Pose(px: 5)));
            Assert.Equal(SendKind.Unreliable, s.Update(0.06f, Pose(px: 5)));  // same pose, still differs from last sent
        }

        [Fact]
        public void Stance_or_flag_change_bypasses_the_rate_limit()
        {
            var s = Make();
            s.Update(0f, Pose(stance: 1));
            Assert.Equal(SendKind.Unreliable, s.Update(0.001f, Pose(stance: 2)));
            Assert.Equal(SendKind.Unreliable, s.Update(0.002f, Pose(stance: 2, flags: 3)));
        }

        [Fact]
        public void Motion_restarts_the_settle_timer()
        {
            var s = Make();
            s.Update(0f, Pose(px: 0));
            s.Update(0.06f, Pose(px: 5));                                    // moved, sent
            Assert.Equal(SendKind.None, s.Update(0.30f, Pose(px: 5)));        // only 0.24 s since move
            Assert.Equal(SendKind.Reliable, s.Update(0.37f, Pose(px: 5)));
        }

        [Fact]
        public void Reset_resends_the_current_pose()
        {
            var s = Make();
            s.Update(0f, Pose());
            s.Reset();
            Assert.Equal(SendKind.Unreliable, s.Update(0.01f, Pose()));
        }

        [Fact]
        public void Configure_changes_the_rate_on_the_fly()
        {
            var s = Make();
            s.Update(0f, Pose(px: 0));
            s.Configure(0.5f, Settle, Keepalive);
            Assert.Equal(SendKind.None, s.Update(0.1f, Pose(px: 5)));
            Assert.Equal(SendKind.Unreliable, s.Update(0.5f, Pose(px: 5)));
        }
    }
}
