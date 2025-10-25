using AirportCEOTweaksCore.lib;
using AirportCEOTweaksCore.Util;
using BepInEx.Configuration;
using Mono.CompilerServices.SymbolWriter;

namespace AirportCEOTweaksCore
{

    public class AirportCEOTweaksCoreConfig
    {

        //public static ConfigEntry<bool> LiveryLogs { get; private set; }
        //public static ConfigEntry<string> PathToCrosshairImage { get; private set; }
        public static ConfigEntry<bool> ValidateJsonManual { get; private set; }

        // Struture repair removed (to an external mod maybe)

        internal static void SetUpConfig()
        {
            //LiveryLogs = ConfigRef.Bind("Debug", "Livery Author Log Files", false, "Enable/Disable extra log files for livery authors to debug active liveries");
            //PathToCrosshairImage = ConfigRef.Bind("Debug", "Path to crosshair", "", "Path to crosshair for mod devs. If empty function will not work");
            ValidateJsonManual = ConfigRef.Bind("Validating JSON", "Validate JSON Now", false, "Set to true to validate all aircraft JSON files. Will automatically reset to false after validation. Check logs for results!");
        }

        private static ConfigFile ConfigRef => AirportCEOTweaksCore.ConfigReference;
    }
}