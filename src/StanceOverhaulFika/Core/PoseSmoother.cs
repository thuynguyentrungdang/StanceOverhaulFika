using System;

namespace StanceOverhaulFika.Core
{
    /// <summary>
    /// Frame-rate independent exponential smoothing of a remote player's stance pose, plus a 0..1 weight
    /// that fades the whole stance in/out (no weapon in hands, stale data, mod disabled).
    /// </summary>
    public sealed class PoseSmoother
    {
        private Pose6 _current;
        private Pose6 _target;
        private float _weight;

        public Pose6 Target => _target;
        public Pose6 Current => _current;
        public float Weight => _weight;

        /// <summary>Pose to add to the weapon, already weighted.</summary>
        public Pose6 Applied => Pose6.Scale(_current, _weight);

        public void SetTarget(in Pose6 target) => _target = target;

        public void Snap(in Pose6 pose)
        {
            _target = pose;
            _current = pose;
        }

        public static float Factor(float dt, float speed)
        {
            if (dt <= 0f || speed <= 0f)
                return 0f;
            return 1f - (float)Math.Exp(-speed * dt);
        }

        public void Tick(float dt, float poseSpeed, float weightTarget, float weightSpeed)
        {
            _current = Pose6.Lerp(_current, _target, Factor(dt, poseSpeed));
            _weight += (weightTarget - _weight) * Factor(dt, weightSpeed);
        }
    }
}
