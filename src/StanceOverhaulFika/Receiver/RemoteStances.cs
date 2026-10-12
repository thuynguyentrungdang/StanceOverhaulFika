using System.Collections.Generic;
using EFT;
using EFT.Animations;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using StanceOverhaulFika.Net;
using UnityEngine;

namespace StanceOverhaulFika.Receiver
{
    /// <summary>
    /// Keeps one <see cref="RemoteStance"/> per remote NetId, ties it to that player's
    /// ProceduralWeaponAnimation, and smooths it every frame. The Harmony patch reads the result.
    /// </summary>
    internal static class RemoteStances
    {
        // sender sends a keepalive every second; past this the sender is gone, fade the stance out
        private const float StaleSeconds = 5f;
        private const float WeightSpeed = 10f;

        private static readonly Dictionary<int, RemoteStance> ByNet = new Dictionary<int, RemoteStance>();
        private static readonly Dictionary<ProceduralWeaponAnimation, RemoteStance> ByPwa = new Dictionary<ProceduralWeaponAnimation, RemoteStance>();
        private static readonly List<int> Gone = new List<int>();

        private static CoopHandler _handler;

        internal static int Count => ByNet.Count;

        internal static void Clear()
        {
            ByNet.Clear();
            ByPwa.Clear();
            _handler = null;
        }

        internal static void Remove(int netId)
        {
            if (!ByNet.TryGetValue(netId, out RemoteStance state))
                return;
            Unmap(state);
            ByNet.Remove(netId);
        }

        internal static void Apply(in StancePosePacket packet)
        {
            if (!Plugin.Enabled.Value)
                return;

            if (_handler == null && !CoopHandler.TryGetCoopHandler(out _handler))
                return;

            // our own stance echoed back is meaningless
            if (_handler.MyPlayer != null && _handler.MyPlayer.NetId == packet.NetId)
                return;

            if (!ByNet.TryGetValue(packet.NetId, out RemoteStance state))
            {
                state = new RemoteStance(packet.NetId);
                ByNet.Add(packet.NetId, state);
                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo($"first stance packet from netId {packet.NetId}");
            }

            state.Accept(in packet, Time.unscaledTime);
        }

        internal static bool TryGetByPwa(ProceduralWeaponAnimation pwa, out RemoteStance state)
        {
            state = null;
            return ByPwa.Count != 0 && pwa != null && ByPwa.TryGetValue(pwa, out state);
        }

        internal static void Tick(float dt)
        {
            if (ByNet.Count == 0)
                return;

            if (_handler == null && !CoopHandler.TryGetCoopHandler(out _handler))
                return;

            float now = Time.unscaledTime;
            float poseSpeed = Plugin.SmoothSpeed.Value;
            bool enabled = Plugin.Enabled.Value;

            foreach (KeyValuePair<int, RemoteStance> pair in ByNet)
            {
                RemoteStance state = pair.Value;

                if (!_handler.Players.TryGetValue(state.NetId, out FikaPlayer player) || player == null || player.IsYourPlayer)
                {
                    // not spawned yet (or already despawned): nothing to attach to
                    Unmap(state);
                    state.Smoother.Tick(dt, poseSpeed, 0f, WeightSpeed);
                    if (now - state.LastReceived > StaleSeconds)
                        Gone.Add(state.NetId);
                    continue;
                }

                ProceduralWeaponAnimation pwa = player.ProceduralWeaponAnimation;
                if (!ReferenceEquals(pwa, state.Pwa))
                {
                    Unmap(state);
                    if (pwa != null)
                    {
                        state.Pwa = pwa;
                        ByPwa[pwa] = state;
                    }
                }

                bool stale = now - state.LastReceived > StaleSeconds;
                bool hasGun = player.HealthController != null
                    && player.HealthController.IsAlive
                    && player.HandsController is Player.FirearmController;

                float weightTarget = enabled && !stale && hasGun ? 1f : 0f;
                state.Smoother.Tick(dt, poseSpeed, weightTarget, WeightSpeed);
            }

            for (int i = 0; i < Gone.Count; i++)
                Remove(Gone[i]);
            Gone.Clear();
        }

        private static void Unmap(RemoteStance state)
        {
            if (state.Pwa != null && ByPwa.TryGetValue(state.Pwa, out RemoteStance mapped) && ReferenceEquals(mapped, state))
                ByPwa.Remove(state.Pwa);
            state.Pwa = null;
        }
    }
}
