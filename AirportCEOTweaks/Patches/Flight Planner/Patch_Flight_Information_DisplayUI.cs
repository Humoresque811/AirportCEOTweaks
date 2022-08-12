using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;

namespace AirportCEOTweaks
{
    [HarmonyPatch(typeof(FlightInformationDisplayUI))]
    static class Patch_Flight_Information_DisplayUI
    {

        [HarmonyPatch(typeof(FlightInformationDisplayUI), "LoadPanel", new Type[] { typeof(FlightSlotContainerUI) })]
        public static void Postfix(FlightSlotContainerUI flightSlotContainer, FlightInformationDisplayUI __instance) //PlannerChangesMod
        {
            //rescedule changes

            if (AirportCEOTweaksConfig.plannerChanges == false) { return; }

            Button button = __instance.transform.Find("FlightAllocationButtons").Find("RescheduleFlightButton").GetComponent<Button>();
            button.interactable = true;
        }

        [HarmonyPatch("SetDisplayAsFlightPlanner")]
        [HarmonyPrefix]
        public static bool RefreshAsPlanner(FlightModel flight)
        {
            SingletonNonDestroy<ModsController>.Instance.GetExtensions(flight as CommercialFlightModel, out Extend_CommercialFlightModel ecfm, out Extend_AirlineModel eam);
            if (ecfm != null)
            {
                ecfm.RefreshTypes();
                ecfm.EvaluateServices();
            }
            return true;
        }
        [HarmonyPatch("SetDisplayAsFlightPlanner")]
        [HarmonyPostfix]
        public static void AddDescriptionPlanner(FlightModel flight, FlightSlotContainerUI __instance)
        {
            if (flight is CommercialFlightModel)
            {

                TextMeshProUGUI FrqValueText = __instance.transform.Find("FlightInfo").Find("FlightFrequencyValueText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI FrqText = __instance.transform.Find("FlightInfo").Find("FlightFrequencyText").GetComponent<TextMeshProUGUI>();

                FrqText.text = "Flight Type:";

                SingletonNonDestroy<ModsController>.Instance.GetExtensions(flight as CommercialFlightModel, out Extend_CommercialFlightModel ecfm, out Extend_AirlineModel eam);
                if (ecfm != null)
                {
                    FrqValueText.text = ecfm.GetDescription(true, true, false, false); //wont work with international = true
                }
            }
        }
        [HarmonyPatch("SetDisplayAsFlightInWorld")]
        [HarmonyPrefix]
        public static bool RefreshAsWorld(FlightModel flight)
        {
            SingletonNonDestroy<ModsController>.Instance.GetExtensions(flight as CommercialFlightModel, out Extend_CommercialFlightModel ecfm, out Extend_AirlineModel eam);
            if (ecfm != null)
            {
                ecfm.RefreshTypes();
                ecfm.EvaluateServices();
            }
            return true;
        }
        [HarmonyPatch("SetDisplayAsFlightInWorld")]
        [HarmonyPostfix]
        public static void AddDescriptionWorld(FlightModel flight, FlightSlotContainerUI __instance)
        {
            if (flight is CommercialFlightModel)
            {

                TextMeshProUGUI FrqValueText = __instance.transform.Find("FlightInfo").Find("FlightFrequencyValueText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI FrqText = __instance.transform.Find("FlightInfo").Find("FlightFrequencyText").GetComponent<TextMeshProUGUI>();

                FrqText.text = "Flight Type:";

                SingletonNonDestroy<ModsController>.Instance.GetExtensions(flight as CommercialFlightModel, out Extend_CommercialFlightModel ecfm, out Extend_AirlineModel eam);
                if (ecfm != null)
                {
                    FrqValueText.text = ecfm.GetDescription(true, true, false, false);
                }
            }
        }


    }

   
}