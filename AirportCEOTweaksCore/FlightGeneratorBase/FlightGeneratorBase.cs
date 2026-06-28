using AirportCEOModLoader.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace AirportCEOTweaksCore;

public abstract class FlightGeneratorBase
{
    protected List<string> airlinesShownError = new();

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
    public FlightGeneratorAction GenerateFlight(AirlineModel airlineModel, bool isEmergency, bool isAmbulance)
    {
        FlightGeneratorResults flightGeneratorResults;
        try
        {
            GenerateFlightModel(airlineModel, isEmergency, isAmbulance, out flightGeneratorResults);
        }
        catch (Exception ex)
        {
            // Fail safe
            AirportCEOTweaksCore.LogError($"Flight generation using generator \"{GeneratorName}\" failed!! {ExceptionUtils.ProccessException(ex)}");
            if (!airlinesShownError.Contains(airlineModel.businessName))
            {
                DialogUtils.QueueDialog($"An error occured during flight generation, while using the \"{GeneratorName}\" generation system for airline \"{airlineModel.businessName}\". " +
                    $"Vanilla generation will be used instead. If this is a reoccuring issue, contact the mod creator.");
                airlinesShownError.Add(airlineModel.businessName);
            };
            return FlightGeneratorAction.UseVanillaGeneration;
        }

        if (flightGeneratorResults.action == FlightGeneratorAction.AllocateFlights)
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

        if (flightGeneratorResults.shouldShowMessage)
        {
            if (Singleton<ModsController>.Instance.flightGenerator.GetErrorNote(airlineModel, out string message))
            {
                DialogUtils.QueueDialog(message);
            }
        }

        return flightGeneratorResults.action; // Pass forward the action so that the patch knows where to direct traffic
    }

    // Main thing that new implementations are going to need to focus on implementing
    public abstract void GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out FlightGeneratorResults flightGeneratorResults);
}
