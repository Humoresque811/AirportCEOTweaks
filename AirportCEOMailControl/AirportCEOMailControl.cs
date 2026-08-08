using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AirportCEOMailControl
{
    [BepInPlugin("org.airportceomailcontrol.Guusje2", "AirportCEOMailControl", "1.0.0")]
    [BepInDependency("org.airportceomodloader.humoresque")]
    [BepInDependency("org.airportceotweakscore.zeke")]
    public class AirportCEOMailControl : BaseUnityPlugin
    {
        public static AirportCEOMailControl Instance { get; private set; }
        internal static Harmony Harmony { get; private set; }
        internal static ManualLogSource TweaksLogger { get; private set; }
        internal static ConfigFile ConfigReference { get; private set; }

        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony.PatchAll();

            Instance = this;
            TweaksLogger = Logger;
            ConfigReference = Config;

            // Config
            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} is setting up config.");
            AirportCEOMailControlConfig.SetUpConfig();
            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} finished setting up config.");
        }

        // This is code for BepInEx logging, which Tweaks doesn't really use. Here if necessary
        internal static void Log(string message) => LogInfo(message);
        internal static void LogInfo(string message) => TweaksLogger.LogInfo(message);
        internal static void LogError(string message) => TweaksLogger.LogError(message);
        internal static void LogWarning(string message) => TweaksLogger.LogWarning(message);
        internal static void LogDebug(string message) => TweaksLogger.LogDebug(message);
    }
}

