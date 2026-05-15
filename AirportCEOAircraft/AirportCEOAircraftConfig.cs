using AirportCEOTweaksCore.lib;
using BepInEx.Configuration;
using Mono.CompilerServices.SymbolWriter;
using System;
using System.Collections.Generic;

namespace AirportCEOAircraft
{

    public class AirportCEOAircraftConfig
    {

        public static ConfigEntry<bool> LiveryLogs { get; private set; }
        public static ConfigEntry<string> PathToCrosshairImage { get; private set; }
        public static ConfigEntry<DownscaleEnums.DownscaleLevel> DownscaleLevel { get; private set; }

        // Struture repair removed (to an external mod maybe)

        internal static void SetUpConfig()
        {
            DownscaleLevel = ConfigRef.Bind("General", "Downscaling Level", DownscaleEnums.DownscaleLevel.Downscale2X, 
                "Amount to downscale the texture by (per axis, so 2x means 2x less RAM/VRAM usage, but you get 4x less quality)");

            LiveryLogs = ConfigRef.Bind("Debug", "Livery Author Log Files", false, SetupAdvancedConfigDescription("Enable/Disable extra log files for livery authors to debug active liveries"));
            PathToCrosshairImage = ConfigRef.Bind("Debug", "Path to crosshair", "", SetupAdvancedConfigDescription("Path to crosshair for mod devs. If empty function will not work"));
        }

        private static ConfigFile ConfigRef => AirportCEOAircraft.ConfigReference;

        private static ConfigDescription SetupAdvancedConfigDescription(string description)
        {
            return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdvanced = true });
        }
    }
}