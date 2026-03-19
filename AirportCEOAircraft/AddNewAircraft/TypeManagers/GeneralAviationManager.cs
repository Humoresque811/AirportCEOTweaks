using AirportCEOTweaksCore;
using System;

namespace AirportCEOAircraft.AddNewAircraft.TypeManagers;

internal static class GeneralAviationManager
{
    public static void HandleGeneralAviation(int idIndex, AircraftTypeData aircraftTypeData)
    {
        if (aircraftTypeData.isGeneralAviation == null)
        {
            return;
        }

        var instance = Singleton<AirTrafficController>.Instance;

        var isGeneralAviation = aircraftTypeData.isGeneralAviation.Length > idIndex ? aircraftTypeData.isGeneralAviation[idIndex] : aircraftTypeData.isGeneralAviation[0];

        AirportCEOAircraft.TweaksLogger.LogMessage($"isGeneralAviation: {isGeneralAviation}. Plane: {aircraftTypeData.id[idIndex]}");

        var planeName = aircraftTypeData.id[idIndex];

        if (isGeneralAviation == true)
        {
            instance.GAAircraft.aircraft = AddGAAircraft(planeName);
        }

        if (isGeneralAviation == false)
        {
            instance.GAAircraft.aircraft = RemoveGAAircraft(planeName);
        }
    }

    private static string[] AddGAAircraft(string newPlane)
    {
        var instance = Singleton<AirTrafficController>.Instance;
        var existingGAAircrafts = instance.GAAircraft.aircraft;

        if (existingGAAircrafts == null)
        {
            return new[] { newPlane };
        }

        // newPlane already exists
        if (Array.Exists(existingGAAircrafts, plane => plane == newPlane))
        {
            return existingGAAircrafts;
        }

        var newAircraftList = new string[existingGAAircrafts.Length + 1];
        Array.Copy(existingGAAircrafts, newAircraftList, existingGAAircrafts.Length);
        newAircraftList[existingGAAircrafts.Length] = newPlane;
        return newAircraftList;
    }

    private static string[] RemoveGAAircraft(string deletePlane)
    {
        var instance = Singleton<AirTrafficController>.Instance;
        var existingGAAircrafts = instance.GAAircraft.aircraft;

        // deletePlane doesn't exist
        if (existingGAAircrafts == null || !Array.Exists(existingGAAircrafts, plane => plane == deletePlane))
        {
            return existingGAAircrafts;
        }

        // After deleting the only plane, list is empty
        if (existingGAAircrafts.Length == 1)
        {
            return Array.Empty<string>();
        }

        var newAircraftList = new string[existingGAAircrafts.Length - 1];
        var index = 0;
        for (var i = 0; i < existingGAAircrafts.Length; i++)
        {
            var existingGaPlane = existingGAAircrafts[i];
            if (existingGaPlane == null || existingGaPlane == deletePlane)
            {
                continue;
            }
            newAircraftList[index++] = existingGaPlane;
        }
        return newAircraftList;
    }
}
