using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace AirportCEOTweaksCore;

public abstract class FlightGeneratorBase
{
    // This is here to prevent a stack overflow infinite loop with the default flight generator. Keep as false unless specifically needed
    public virtual bool OverrideHarmonyPrefix { get; set; } = false;

    // Flight generator must provide the name
    public abstract string GeneratorName { get; }

    // The error message to display if generation fails - if blank, then no additional info is printed
    public virtual bool GetErrorNote(AirlineModel model, out string message)
    {
        message = null;
        return false;
    }

    // This takes out the usage of the flight model part from GenerateFlightModel(), ensuring more safety and making the purpose of the GenerateFlightModel() method far clearer
    public FlightGeneratorResultAction GenerateFlight(AirlineModel airlineModel, bool isEmergency, bool isAmbulance)
    {
        GenerateFlightModel(airlineModel, isEmergency, isAmbulance, out FlightGeneratorResults flightGeneratorResults);

        if (flightGeneratorResults.action == FlightGeneratorResultAction.AllocateFlights)
        {
            foreach (CommercialFlightModel flightModel in flightGeneratorResults.commercialFlightModels)
            {

                if (Singleton<AirTrafficController>.Instance.referenceToFlight.ContainsKey(flightModel.referenceID))
                {
                    // This means that the flight model has in fact already been added
                    continue;
                }

                // Default additions to do, if not done already
                Singleton<AirTrafficController>.Instance.AddToFlightList(flightModel);
                airlineModel.flightList.Add(flightModel.referenceID);
                airlineModel.flightListObjects.Add(flightModel);
            }
        }

        if (flightGeneratorResults.action == FlightGeneratorResultAction.UseVanillaGeneration)
        {
            if (Singleton<ModsController>.Instance.flightGenerator.GetErrorNote(airlineModel, out string message))
            {
                AirportCEOModLoader.Core.DialogUtils.QueueDialog(message);
            }
        }

        return flightGeneratorResults.action; // Pass forward the action so that the patch knows where to direct traffic
    }

    // Main thing that new implementations are going to need to focus on implementing
    public abstract void GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out FlightGeneratorResults flightGeneratorResults);
}
