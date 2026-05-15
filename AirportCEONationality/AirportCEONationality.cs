using UnityEngine;
using BepInEx;
using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using AirportCEOTweaksCore;
using AirportCEOModLoader.Core;

namespace AirportCEONationality
{

    [BepInPlugin("org.airportceonationality.zeke", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("org.airportceomodloader.humoresque")]
    [BepInIncompatibility("org.airportceotweaks.zeke")]
    [BepInDependency("org.airportceotweakscore.zeke")]
    public class AirportCEONationality : BaseUnityPlugin
    {
        public static AirportCEONationality Instance { get; private set; }
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
            AirportCEONationalityConfig.SetUpConfig();
            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} finished setting up config.");

            GameObject child = Instantiate(new GameObject());
            child.transform.SetParent(null);
            child.name = "ACEONationalitySysActive";

            gameObject.AddComponent<RouteGenerationController>();
        }

        private void Start()
        {
            ModLoaderInteractionHandler.SetUpInteractions();
            TryShowAlert();
            LogInfo("Tweaks Nationality finished start");
        }

        private void TryShowAlert()
        {
            try
            {
                Version versionOfCore = AirportCEOTweaksCore.AirportCEOTweaksCore.Instance.Info.Metadata.Version;
                if (versionOfCore.CompareTo(new Version("3.9.9.0")) < 0)
                {
                    DialogUtils.QueueDialog("!! ALERT !!\nYou are using Tweaks Nationality with an old (pre V4) version of Tweaks Core! " +
                        "This WILL lead to errors! Please update Tweaks Core by either unsubscribing and resubscribing if you own the game " +
                        "via Steam, or manually updating it from GitHub. ");
                    TweaksLogger.LogFatal("Tweaks Core is not a pre V4 version!! This will cuase errors.");
                    AirportCEONationalityConfig.EnableNationalityFlightGeneration.Value = false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to check Tweaks Core version to check for errors. {ExceptionUtils.ProccessException(ex)}");
            }
        }

        // This is code for BepInEx logging, which Tweaks doesn't really use. Here if necessary
        internal static void Log(string message) => LogInfo(message);
        internal static void LogInfo(string message) => TweaksLogger.LogInfo(message);
        internal static void LogError(string message) => TweaksLogger.LogError(message);
        internal static void LogWarning(string message) => TweaksLogger.LogWarning(message);
        internal static void LogDebug(string message) => TweaksLogger.LogDebug(message);
    }
}