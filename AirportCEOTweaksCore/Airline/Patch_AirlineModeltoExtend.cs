using System;
using System.Collections;
using UnityEngine;
using HarmonyLib;
using BepInEx;

namespace AirportCEOTweaksCore
{
	[HarmonyPatch(typeof(AirlineModel))]
	static class Patch_AirlineModeltoExtend
	{
		[HarmonyPatch(typeof(AirlineModel), MethodType.Constructor, new Type[] { typeof(Airline) })]
		[HarmonyPostfix]
		public static void Patch_Ctor(ref AirlineModel __instance)
		{
			if (__instance as AirlineModelExtended != null)
            {
				//((AirlineModelExtended)__instance).Refresh();
				return;// true;
            }
			Debug.Log("Patch to extend " + __instance.businessName + " is triggered (ctor)");
			__instance.ExtendAirlineModel(ref __instance);
			//if (__instance as AirlineModelExtended != null) { ((AirlineModelExtended)__instance).Refresh(); return false; }
			//else { Debug.LogError("AirlineModelExtended turned null before could refresh"); return true; }
			//return false;
		}

		[HarmonyPatch("GenerateFlight")]
		[HarmonyPostfix]
		public static void Patch_GenerateFlightToExtend(ref AirlineModel __instance)
		{
			if (__instance as AirlineModelExtended != null)
			{
				((AirlineModelExtended)__instance).Refresh();
				return;// true;
			}
			AirportCEOTweaksCore.LogDebug($"Patch to extend {__instance.businessName} triggered (from Generate Flight)");
			__instance.ExtendAirlineModel(ref __instance);
			//if (__instance as AirlineModelExtended != null) { ((AirlineModelExtended)__instance).Refresh(); return false; }
			//else { Debug.LogError("AirlineModelExtended turned null before could refresh"); return true; }
			//return false;
		}

		[HarmonyPatch("GenerateFlight")]
		[HarmonyPrefix]
		public static bool Patch_GenerateFlight(AirlineModel __instance, bool isEmergency, bool isAmbulance, ref bool __result)
		{
			/*********** The return value is whether to proceed with default generation or not, the __result is success or not ***********/

			if (Singleton<ModsController>.Instance.flightGenerator.OverrideHarmonyPrefix)
            {
				return true; // This is to avoid infinite loops with our default fight generator (it has to call this method, but we want to let it execute)
            }

			AirportCEOTweaksCore.LogDebug($"Generating Flight for \"{__instance.businessName}\"");
			FlightGeneratorResultAction action = Singleton<ModsController>.Instance.flightGenerator.GenerateFlight(__instance, isEmergency, isAmbulance);
			if (action == FlightGeneratorResultAction.AllocateFlights || action == FlightGeneratorResultAction.AlreadyAllocated) // Full success
            {
				__result = true; // This matters only if we return false, now we assign it
				return false;
            }
			else if (action == FlightGeneratorResultAction.DontCreate) // Dont create, fail
            {
				__result = false;
				return false;
            }
			else
			{
				return true; // continue with vanilla system!
			}
		}
	}
}