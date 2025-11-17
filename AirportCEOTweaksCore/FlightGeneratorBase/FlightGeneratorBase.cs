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

    // This takes out the usage of the flight model part from GenerateFlightModel(), ensuring more safety and making the purpose of the GenerateFlightModel() method far clearer
    public bool GenerateFlight(AirlineModel airlineModel, bool isEmergency, bool isAmbulance)
    {
        if (GenerateFlightModel(airlineModel, isEmergency, isAmbulance, out List<CommercialFlightModel> flightModels))
        {
            if (flightModels == null)
            {
                return false;
            }

            foreach (CommercialFlightModel flightModel in flightModels)
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
            return true;
        }

        return false;
    }

    // Main thing that new implementations are going to need to focus on implementing
    public abstract bool GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out List<CommercialFlightModel> commercialFlightModel);
}
