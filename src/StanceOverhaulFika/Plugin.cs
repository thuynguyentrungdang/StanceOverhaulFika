using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Fika.Core.Main.Players;
using HarmonyLib;
using StanceOverhaulFika.Net;
using StanceOverhaulFika.Patches;
using StanceOverhaulFika.Receiver;
using StanceOverhaulFika.Sender;
using UnityEngine;

namespace StanceOverhaulFika
{
    /// <summary>
    /// Shows every Fika player's Stance Overhaul stance on every other client.
    /// Fika is required. Stance Overhaul is optional: without it this install still SEES other
    /// players' stances and relays them (that is what a headless host needs), it just sends none.
    /// </summary>
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(StanceOverhaulGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.kobethuy.stance-overhaul-fika";
        public const string MOD_NAME = "StanceOverhaulFika";
        public const string MOD_VERSION = "0.1.0";

        // kept as a string so this assembly does not load Stance Overhaul when it is absent
        private const string StanceOverhaulGuid = "com.fontaine.spt-stance-overhaul";

        internal static ManualLogSource Log;
        internal static bool StanceOverhaulPresent;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> SendRate;
        internal static ConfigEntry<float> Keepalive;
        internal static ConfigEntry<float> SmoothSpeed;
        internal static ConfigEntry<bool> DebugLog;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Send your stance and show other players' stances. Every player in the raid (and the headless host) should use the same setting.");
            SendRate = Config.Bind("Network", "Send rate (Hz)", 20,
                new ConfigDescription("Max stance updates per second while your stance is moving.", new AcceptableValueRange<int>(1, 60)));
            Keepalive = Config.Bind("Network", "Keepalive (seconds)", 1f,
                new ConfigDescription("Resend an unchanged stance this often. Lets late joiners see it and heals lost packets.", new AcceptableValueRange<float>(0.25f, 5f)));
            SmoothSpeed = Config.Bind("Display", "Smoothing speed", 25f,
                new ConfigDescription("How quickly other players' guns follow incoming stance updates. Higher = snappier.", new AcceptableValueRange<float>(5f, 60f)));
            DebugLog = Config.Bind("Debug", "Verbose log", false, "Log extra networking info.");

            StanceOverhaulPresent = Chainloader.PluginInfos.ContainsKey(StanceOverhaulGuid);
            if (!StanceOverhaulPresent)
                Log.LogInfo("Stance Overhaul not installed: receive/relay only, your own stance will not be sent");

            StanceNet.Initialize();
            new Harmony(MOD_GUID).PatchAll(typeof(ZeroAdjustmentsPatch));

            FikaPlayer.OnPlayerDestroyed += OnPlayerDestroyed;

            Log.LogInfo($"{MOD_NAME} {MOD_VERSION} loaded");
        }

        private void OnDestroy()
        {
            FikaPlayer.OnPlayerDestroyed -= OnPlayerDestroyed;
        }

        // despawned/dead player: drop its stance so the weapon pose cannot linger or be reapplied
        private static void OnPlayerDestroyed(FikaPlayer player)
        {
            if (player != null)
                RemoteStances.Remove(player.NetId);
        }

        private void Update()
        {
            StanceNet.Tick();
            StanceSender.Tick();
            RemoteStances.Tick(Time.deltaTime);
        }
    }
}
