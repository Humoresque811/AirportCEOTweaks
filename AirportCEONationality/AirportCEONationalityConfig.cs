using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace AirportCEONationality
{
    public class AirportCEONationalityConfig
    {
        internal static ConfigEntry<bool> EnableNationalityFlightGeneration { get; private set; }
        internal static ConfigEntry<float> VintageGenerationMultiplier { get; private set; }
        internal static ConfigEntry<float> InternationalGenerationMultiplier { get; private set; }
        internal static ConfigEntry<bool> ExtraDebugLogs{ get; private set; }
        internal static ConfigEntry<NationalityFallbackRule> FallbackGenerationMode { get; private set; }

        internal static void SetUpConfig()
        {
            EnableNationalityFlightGeneration = ConfigRef.Bind("General", "Enable Nationality Flight Generation", true, "Switch flight generation to nationality flight generation?");

            VintageGenerationMultiplier = ConfigRef.Bind("General", "Vintage Generation Multiplier", 0.5f, new ConfigDescription("Reduce (or increase) the chance of vintage planes generating", 
                new AcceptableValueRange<float>(0, 2f)));
            InternationalGenerationMultiplier = ConfigRef.Bind("General", "International Generation Multiplier", 0.5f, new ConfigDescription("Reduce (or increase) the chance of international " +
                "flights generating. Ex: 0.5 to halve the chance of international flights generating compared to domestic", new AcceptableValueRange<float>(0, 2f)));

            FallbackGenerationMode = ConfigRef.Bind("General", "Fallback Generation Mode", NationalityFallbackRule.FallbackVanillaNotify, "What to do if the mod is unable to generate flights " +
                "using the realistic nationality generation system. Fallback to vanilla & Notify is recommended");
            EnableNationalityFlightGeneration.SettingChanged += NationalityFlightGenerator.ToggleGenerator;

            ExtraDebugLogs = ConfigRef.Bind("Debug", "Extra Debug Logs", false, "Logs lots of extra info about flights generating.");
        }

        private static ConfigFile ConfigRef => AirportCEONationality.ConfigReference;
    }
}