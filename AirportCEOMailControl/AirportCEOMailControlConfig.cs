using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AirportCEOMailControl
{
    internal class AirportCEOMailControlConfig
    {
        internal static ConfigEntry<bool> EnableOperationalReportMail { get; private set; }
        internal static ConfigEntry<bool> EnableFinancialReportMail { get; private set; }
        internal static ConfigEntry<bool> EnableEmergencyReportMail { get; private set; }
        internal static ConfigEntry<bool> EnableContractNegotiationReminderMail { get; private set; }
        internal static ConfigEntry<bool> DebugMode { get; private set; }
        internal static void SetUpConfig()
        {
            EnableOperationalReportMail = ConfigRef.Bind("Mail", "Enable operational report e-mail", true, "Whether to enable operational report mail.");
            EnableFinancialReportMail = ConfigRef.Bind("Mail", "Enable financial report e-mail", true, "Whether to enable financial report mail.");
            DebugMode = ConfigRef.Bind("Debug", "Debug mode", false, "Whether to enable debug mode, adds verbose logging.");
            //EnableEmergencyReportMail = ConfigRef.Bind("Mail", "EnableEmergencyReportMail", true, "Whether to enable emergency report mail.");
            EnableContractNegotiationReminderMail = ConfigRef.Bind("Mail", "Enable contract negotiation reminder e-mail", true, "Whether to enable contract negotiation reminder mail.");
        }

        private static ConfigFile ConfigRef => AirportCEOMailControl.ConfigReference;
    }
}
