using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirportCEOTweaksCore;

public abstract class FlightGeneratorBase
{
    // This is here to prevent a stack overflow infinite loop with the default flight generator. Keep as false unless specifically needed
    public virtual bool OverrideHarmonyPrefix { get; set; } = false;
    // Flight generator must provide the name
    public abstract string GeneratorName { get; } 

    // This takes out the usage of the flight model part from GenerateFlightModel(), ensuring more safety and making the purpose of the GenerateFlightModel() method far clearer
    public bool GenerateFlight(AirlineModel airlineModel, bool isEmergency, bool isAmbulance)
    {
        if (GenerateFlightModel(airlineModel, isEmergency, isAmbulance, out CommercialFlightModel flightModel))
        {
            if (flightModel == null)
            {
                AirportCEOTweaksCore.LogError($"Flight model returned by a flight model generator \"{GeneratorName}\" is null, " +
                    $"despite the generator returning true indicating success! Flight not added, returning failure");
                return false;
            }

            if (Singleton<AirTrafficController>.Instance.referenceToFlight.ContainsKey(flightModel.referenceID))
            {
                // This means that the flight model has in fact already been added (this should *only* happen when using the default flight generator, as we cant change it to use our new format
                return true;
            }

            // Default additions to do, if not done already
            Singleton<AirTrafficController>.Instance.AddToFlightList(flightModel);
            airlineModel.flightList.Add(flightModel.referenceID);
            airlineModel.flightListObjects.Add(flightModel);
            return true;
        }

        return false;
    }

    // Main thing that new implementations are going to need to focus on implementing
    public abstract bool GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out CommercialFlightModel commercialFlightModel);
}
