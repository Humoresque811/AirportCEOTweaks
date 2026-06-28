using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AirportCEOTweaksCore;

[HarmonyPatch]
internal class Patch_ContractViewing
{
    [HarmonyPatch(typeof(SelectedContractUI), nameof(SelectedContractUI.SetContractPanelValues))]
    [HarmonyPrefix]
    internal static void PreSetContractValues(SelectedContractUI __instance, BusinessModel business, bool valid, Transform parent)
    {
        if (business is not AirlineModel)
        {
            return;
        }


        AirlineModel model = (AirlineModel)business;
        try
        {
            model.ExtendAirlineModel(ref model);
        }
        catch
        {
            // We dont care this is just to try and fix the incorrect contract issue!
        }
    }
}
