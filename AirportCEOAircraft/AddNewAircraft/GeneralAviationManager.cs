using AirportCEOTweaksCore;
using System;

namespace AirportCEOAircraft.AddNewAircraft;

internal static class GeneralAviationManager
{
    public static void HandleGeneralAviation(int idIndex, AircraftTypeData aircraftTypeData)
    {
        if(aircraftTypeData.isGeneralAviation == null)
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

        var ga = instance.GAAircraft.aircraft;
        AirportCEOAircraft.TweaksLogger.LogMessage($"Folowing aircrafts are GA's");
        for (int j = 0; j < ga.Length; j++)
        {
            AirportCEOAircraft.TweaksLogger.LogMessage($"Loading: {ga[j]}");
        }
    }

    private static string[] AddGAAircraft(string newPlane)
    {
        var instance = Singleton<AirTrafficController>.Instance;
        var existingGAAircrafts = instance.GAAircraft.aircraft;

        // newPlane does already exist
        if (Array.Exists(existingGAAircrafts, plane => plane == newPlane))
        {
            return existingGAAircrafts;
        }

        string[] newAircraftList = new string[existingGAAircrafts.Length + 1];

        var index = 0;
        for (int i = 0; i < existingGAAircrafts.Length; i++)
        {
            var existingGaPlane = existingGAAircrafts[i];

            if (existingGaPlane == null || existingGaPlane == newPlane)
            {
                continue;
            }

            newAircraftList[index] = existingGaPlane;

            index++;
        }

        // Add new GA aircraft
        newAircraftList[index] = newPlane;

        return newAircraftList;
    }

    private static string[] RemoveGAAircraft(string deletePlane)
    {
        var instance = Singleton<AirTrafficController>.Instance;
        var existingGAAircrafts = instance.GAAircraft.aircraft;

        // deletePlane doesn't exist
        if (!Array.Exists(existingGAAircrafts, plane => plane == deletePlane))
        {
            return existingGAAircrafts;
        }

        // existingGAAircraft is empty or after deleting the plane 
        if (existingGAAircrafts.Length == 0 || existingGAAircrafts.Length - 1 == 0)
        {
            return new string[0];
        }

        string[] newAircraftList = new string[existingGAAircrafts.Length - 1];

        var index = 0;
        for (int i = 0; i < existingGAAircrafts.Length; i++)
        {
            var existingGaPlane = existingGAAircrafts[i];

            // Skip the aircraft to delete
            if (existingGaPlane == null || existingGaPlane == deletePlane)
            {
                continue;
            }

            newAircraftList[index] = existingGaPlane;
            index++;
        }

        // If the plane to delete was not found, return the original list
        if (index == existingGAAircrafts.Length)
        {
            return existingGAAircrafts; // No deletion occurred
        }

        return newAircraftList;
    }
}
