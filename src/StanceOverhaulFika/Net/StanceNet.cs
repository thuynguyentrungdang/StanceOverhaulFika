using System;
using System.Collections.Concurrent;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using StanceOverhaulFika.Receiver;
using StanceOverhaulFika.Sender;
using UnityEngine;

namespace StanceOverhaulFika.Net
{
    /// <summary>
    /// Only place that talks to Fika networking. Everything else works on plain data.
    /// Packets are registered per raid (Fika rebuilds its server/client each raid) and handed to the
    /// main thread through a queue before anything touches Unity objects.
    /// </summary>
    internal static class StanceNet
    {
        private const int MaxPacketsPerFrame = 512;

        private static readonly ConcurrentQueue<StancePosePacket> Inbox = new ConcurrentQueue<StancePosePacket>();

        private static volatile bool _startRequested;
        private static volatile bool _endRequested;
        private static bool _active;
        private static bool _registered;
        private static float _nextRetry;

        internal static bool Ready => _active && _registered;

        internal static void Initialize()
        {
            // Fika's UnsubscribeEvent does not remove lambdas, so subscribe once for the whole process
            // and gate on _active instead.
            FikaEventDispatcher.SubscribeEvent<FikaGameCreatedEvent>(OnGameCreated);
            FikaEventDispatcher.SubscribeEvent<FikaGameEndedEvent>(OnGameEnded);
        }

        private static void OnGameCreated(FikaGameCreatedEvent e) => _startRequested = true;

        private static void OnGameEnded(FikaGameEndedEvent e) => _endRequested = true;

        /// <summary>Called every frame from <see cref="Plugin.Update"/>.</summary>
        internal static void Tick()
        {
            if (_endRequested)
            {
                _endRequested = false;
                LeaveRaid();
                Plugin.Log.LogInfo("raid ended, stance sync off");
            }

            if (_startRequested)
            {
                _startRequested = false;
                LeaveRaid();
                _active = true;
                _nextRetry = 0f;
                Plugin.Log.LogInfo($"raid started as {(FikaBackendUtils.IsServer ? "HOST" : "CLIENT")}, headless: {FikaBackendUtils.IsHeadless || FikaBackendUtils.IsHeadlessGame}");
            }

            if (!_active)
                return;

            if (!_registered && Time.unscaledTime >= _nextRetry)
            {
                // the network manager may not exist yet on the frame the game is created
                _nextRetry = Time.unscaledTime + 1f;
                TryRegister();
            }

            int handled = 0;
            while (handled++ < MaxPacketsPerFrame && Inbox.TryDequeue(out StancePosePacket packet))
                RemoteStances.Apply(in packet);
        }

        private static void LeaveRaid()
        {
            _active = false;
            _registered = false;
            while (Inbox.TryDequeue(out _)) { }
            RemoteStances.Clear();
            StanceSender.Reset();
        }

        private static void TryRegister()
        {
            try
            {
                if (FikaBackendUtils.IsServer)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    if (server == null)
                        return;
                    server.RegisterPacket<StancePosePacket>(OnPacket);
                }
                else
                {
                    FikaClient client = Singleton<FikaClient>.Instance;
                    if (client == null)
                        return;
                    client.RegisterPacket<StancePosePacket>(OnPacket);
                }

                _registered = true;
                Plugin.Log.LogInfo("stance packet registered");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"could not register stance packet, stances will NOT sync: {ex}");
            }
        }

        // runs on whatever thread Fika polls on; only queue here
        private static void OnPacket(StancePosePacket packet) => Inbox.Enqueue(packet);

        internal static void Send(ref StancePosePacket packet, bool reliable)
        {
            if (!Ready)
                return;

            DeliveryMethod method = reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable;
            try
            {
                if (FikaBackendUtils.IsServer)
                {
                    // host/headless: Fika sends to every connected peer
                    Singleton<FikaServer>.Instance?.SendData(ref packet, method);
                }
                else
                {
                    // client: broadcast=true makes the host relay it to every other peer
                    Singleton<FikaClient>.Instance?.SendData(ref packet, method, true);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"stance send failed: {ex.Message}");
            }
        }
    }
}
