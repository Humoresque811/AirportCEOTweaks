using System;
using System.Collections;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace AirportCEOAircraft
{
	[HarmonyPatch(typeof(GameController))]
	static class Patch_GameControllerToAddNewPrefabs
	{
		private static bool registered;

		[HarmonyPostfix]
		[HarmonyPatch("Awake")]
		public static void Patch_AddPrefabs(GameController __instance)
		{
			if (registered)
			{
				return;
			}
			registered = true;
			ModLoaderInteractionHandler.AddCoroutineReference(LoadAircraft);
			AirportCEOAircraft.TweaksLogger.LogMessage("Set up aircraft loading callback with mod loader!");
		}

		private static IEnumerator LoadAircraft()
		{
			AircraftAdder adder = Singleton<AirTrafficController>.Instance.gameObject.AddComponent<AircraftAdder>();
			AirportCEOAircraft.TweaksLogger.LogMessage("Added aircraft adder!");
			yield return adder.Initialize();
		}
	}
}
