namespace StanceOverhaulFika.Core
{
    public enum SendKind
    {
        None,
        Unreliable,
        Reliable
    }

    /// <summary>
    /// Decides when the local stance goes on the wire.
    /// - first sample: send
    /// - stance/flag change: send now
    /// - pose moving: send at most every <c>interval</c> seconds (unreliable)
    /// - pose settled for <c>settleDelay</c>: one reliable send so a lost packet cannot leave a stale pose
    /// - otherwise: unreliable keepalive every <c>keepalive</c> seconds (late joiners, packet loss)
    /// </summary>
    public sealed class SendScheduler
    {
        private float _interval;
        private float _settleDelay;
        private float _keepalive;

        private QPose _last;
        private bool _hasLast;
        private float _lastSend;
        private float _lastChange;
        private bool _settledSent;

        public SendScheduler(float interval, float settleDelay, float keepalive)
        {
            Configure(interval, settleDelay, keepalive);
        }

        public void Configure(float interval, float settleDelay, float keepalive)
        {
            _interval = interval;
            _settleDelay = settleDelay;
            _keepalive = keepalive;
        }

        public void Reset()
        {
            _hasLast = false;
            _settledSent = false;
        }

        public SendKind Update(float now, in QPose q)
        {
            if (!_hasLast)
            {
                _hasLast = true;
                _last = q;
                _lastSend = now;
                _lastChange = now;
                _settledSent = false;
                return SendKind.Unreliable;
            }

            if (!q.Equals(_last))
            {
                _lastChange = now;
                _settledSent = false;

                bool discrete = q.Stance != _last.Stance || q.Flags != _last.Flags;
                if (discrete || now - _lastSend >= _interval)
                {
                    _last = q;
                    _lastSend = now;
                    return SendKind.Unreliable;
                }

                // rate limited: keep _last so the next frame still sees a change and sends the latest pose
                return SendKind.None;
            }

            if (!_settledSent && now - _lastChange >= _settleDelay)
            {
                _settledSent = true;
                _lastSend = now;
                return SendKind.Reliable;
            }

            if (now - _lastSend >= _keepalive)
            {
                _lastSend = now;
                return SendKind.Unreliable;
            }

            return SendKind.None;
        }
    }
}
