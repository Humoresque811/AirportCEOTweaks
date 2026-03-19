using AirportCEOTweaksCore;
using System;

namespace AirportCEOAircraft.AddNewAircraft.TypeManagers;

internal static class CopyFromListManager
{
    public static void HandleCopyFromLists(int idIndex, AircraftTypeData aircraftTypeData)
    {
        // Used to determine in which lists the aircraft should be added.
        if (string.IsNullOrEmpty(aircraftTypeData.copyFrom))
        {
            return;
        }

        var atc = Singleton<AirTrafficController>.Instance;
        var copyFrom = aircraftTypeData.copyFrom;
        var newId = aircraftTypeData.id[idIndex];

        if (TryAddToList(atc.supersonics, copyFrom, newId)) return;
        if (TryAddToList(atc.GAHelicopters, copyFrom, newId))
        {
            // Scheduling/runway logic uses helicopters list, not only GAHelicopters so add it to both lists.
            TryAddToList(atc.helicopters, copyFrom, newId);
            return;
        }
        if (TryAddToList(atc.easterns, copyFrom, newId)) return;
        if (TryAddToList(atc.helicopters, copyFrom, newId)) return;
        if (TryAddToList(atc.vintages, copyFrom, newId)) return;
    }

    private static bool TryAddToList(AircraftTypeList list, string copyFrom, string newId)
    {
        if (list?.aircraft == null)
        {
            return false;
        }

        var aircraft = list.aircraft;
        var hasCopyFrom = false;
        var hasNewId = false;

        for (var i = 0; i < aircraft.Length; i++)
        {
            // Whether this list contains the template aircraft we copy from.
            if (aircraft[i] == copyFrom) hasCopyFrom = true;
            // Whether the new aircraft ID is already in the list (no add needed).
            if (aircraft[i] == newId) hasNewId = true;
            if (hasCopyFrom && hasNewId) break;
        }

        // If the template aircraft is not in the list, we won't add the new aircraft
        if (!hasCopyFrom)
        {
            AirportCEOAircraft.TweaksLogger.LogDebug($"[CopyFromList] template {copyFrom} not in list, skip");
            return false;
        }

        if (hasNewId)
        {
            AirportCEOAircraft.TweaksLogger.LogDebug($"[CopyFromList] {newId} already in list, no add");
            return true;
        }

        var newList = new string[aircraft.Length + 1];
        Array.Copy(aircraft, newList, aircraft.Length);
        newList[aircraft.Length] = newId;
        list.aircraft = newList;
        AirportCEOAircraft.TweaksLogger.LogDebug($"[CopyFromList] added {newId} (copyFrom={copyFrom})");
        return true;
    }
}
