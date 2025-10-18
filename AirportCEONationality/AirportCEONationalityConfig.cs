using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Mono.CompilerServices.SymbolWriter;

namespace AirportCEONationality
{
    public class AirportCEONationalityConfig
    {
        internal static ConfigEntry<bool> EnableNationalityFlightGeneration { get; private set; }
        internal static ConfigEntry<bool> ForceGenerateRoutes { get; private set; }

        internal static void SetUpConfig()
        {
            ForceGenerateRoutes = ConfigRef.Bind("General", "Force Route Generation", true, "When enabled, airlines will always generate possible routes even if their configuration " +
                "(home country, hub locations, aircraft fleet) would not allow it. Disabling will lead to more realistic gameplay, however, some custom airlines may not generate any " +
                "flights! Setting currently has no effect (!)");
            EnableNationalityFlightGeneration = ConfigRef.Bind("General", "Enable Nationality Flight Generation", true, "Switch flight generation to nationality flight generation?");
            EnableNationalityFlightGeneration.SettingChanged += NationalityFlightGenerator.ToggleGenerator;
        }

        private static ConfigFile ConfigRef => AirportCEONationality.ConfigReference;
    }
}