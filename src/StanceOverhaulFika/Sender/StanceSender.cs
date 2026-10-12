using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using StanceOverhaulFika.Core;
using StanceOverhaulFika.Net;
using UnityEngine;

namespace StanceOverhaulFika.Sender
{
    /// <summary>Reads the local player's stance from Stance Overhaul and publishes it over Fika.</summary>
    internal static class StanceSender
    {
        private const float SettleDelay = 0.3f;

        private static SendScheduler _scheduler;
        private static CoopHandler _handler;
        private static ushort _seq;

        internal static void Reset()
        {
            _scheduler?.Reset();
            _handler = null;
            _seq = 0;
        }

        internal static void Tick()
        {
            // a headless process has no real player; whatever dummy it spawns must not broadcast stances
            if (!Plugin.Enabled.Value || !Plugin.StanceOverhaulPresent || !StanceNet.Ready || FikaBackendUtils.IsHeadless)
                return;

            if (_handler == null && !CoopHandler.TryGetCoopHandler(out _handler))
                return;

            FikaPlayer me = _handler.MyPlayer;
            if (me == null) // headless host has no local player
                return;

            if (!StanceSource.TryRead(out byte stance, out byte flags, out Pose6 pose))
                return;

            float interval = 1f / Mathf.Clamp(Plugin.SendRate.Value, 1, 60);
            if (_scheduler == null)
                _scheduler = new SendScheduler(interval, SettleDelay, Plugin.Keepalive.Value);
            else
                _scheduler.Configure(interval, SettleDelay, Plugin.Keepalive.Value);

            QPose q = QPose.From(stance, flags, in pose);
            SendKind kind = _scheduler.Update(Time.unscaledTime, in q);
            if (kind == SendKind.None)
                return;

            StancePosePacket packet = new StancePosePacket
            {
                Seq = unchecked(++_seq),
                NetId = me.NetId,
                Pose = q
            };
            StanceNet.Send(ref packet, kind == SendKind.Reliable);
        }
    }
}
