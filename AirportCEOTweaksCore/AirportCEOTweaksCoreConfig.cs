using AirportCEOTweaksCore.lib;
using AirportCEOTweaksCore.Util;
using BepInEx.Configuration;

namespace AirportCEOTweaksCore;

public class AirportCEOTweaksCoreConfig
{
    public static ConfigEntry<bool> ValidateJsonManual { get; private set; }
    public static ConfigEntry<string> CustomWorkshopPath { get; private set; }

    internal static void SetUpConfig()
    {
        ValidateJsonManual = ConfigRef.Bind("Validating JSON", "Validate JSON Now", false, SetupAdvancedConfigDescription("Set to true to validate all aircraft JSON files. Will automatically reset to false after validation. Check logs for results!"));
        CustomWorkshopPath = ConfigRef.Bind("Validating JSON", "Custom Workshop Path (only when workshop path is not the default path)", DirectoryHelpers.GetWorkshopPath(), SetupAdvancedConfigDescription("Path to custom workshop directory. If empty, the default workshop path will be used."));
    }

    private static ConfigFile ConfigRef => AirportCEOTweaksCore.ConfigReference;

    private static ConfigDescription SetupAdvancedConfigDescription(string description)
    {
        return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdvanced = true });
    }
}