using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AirportCEOMailControl
{
    /// <summary>
    /// Disables the generation of negotiation emails if the corresponding config option is disabled.
    /// </summary>
    [HarmonyPatch(typeof(EmailController), nameof(EmailController.GenerateNegotiationEmail))]
    internal class patchGenerateNegotiationEmail
    {

        static bool Prefix(List<BusinessModel> negotiableBusinesses, bool autoNegotiation, int points)
        {
            if (!AirportCEOMailControlConfig.EnableContractNegotiationReminderMail.Value)
            {
                if (AirportCEOMailControlConfig.DebugMode.Value)
                {
                    AirportCEOMailControl.LogInfo("Blocked generation of contract negotiation reminder mail.");
                }
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Disables the generation of performance report emails if the corresponding config option is disabled.
    /// </summary>
    [HarmonyPatch(typeof(EmailController), nameof(EmailController.GeneratePerformanceReport))]
    internal class patchGeneratePerformanceReport
    {
        static bool Prefix(Enums.PerformanceReportType reportType)
        {
            switch (reportType)
            {
                case Enums.PerformanceReportType.Economy:
                    if (!AirportCEOMailControlConfig.EnableFinancialReportMail.Value)
                    {
                        if (AirportCEOMailControlConfig.DebugMode.Value)
                        {
                            AirportCEOMailControl.LogInfo("Blocked generation of financial report mail.");
                        }
                        return false;
                    }
                    break;
                case Enums.PerformanceReportType.Operations:
                    if (!AirportCEOMailControlConfig.EnableOperationalReportMail.Value)
                    {
                        if (AirportCEOMailControlConfig.DebugMode.Value)
                        {
                            AirportCEOMailControl.LogInfo("Blocked generation of operational report mail.");
                        }
                        return false;
                    }
                    break;
            }
            return true;
        }
    }
}
