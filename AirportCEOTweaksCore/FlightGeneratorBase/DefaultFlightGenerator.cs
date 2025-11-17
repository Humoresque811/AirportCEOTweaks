using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AirportCEOTweaksCore;

public class DefaultFlightGenerator : FlightGeneratorBase
{
    public override bool OverrideHarmonyPrefix { get; set; } = false;
    public override string GeneratorName => typeof(DefaultFlightGenerator).Name;

    public override bool GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out List<CommercialFlightModel> flightModel)
    {
        //return airlineModel.ExtendAirlineModel(ref airlineModel).GenerateFlight(isEmergency, isAmbulance);
        Debug.Log("DefaultFlightGeneratorGenerateFlight00");

        OverrideHarmonyPrefix = true;
        bool success = airlineModel.GenerateFlight(isEmergency, isAmbulance);
        OverrideHarmonyPrefix = false;

        // This is a way to make the default generator to work with the new format. We simply get the flightModel we just made against and return that (the base class will handle the duplicate)
        flightModel = null;

        return success;
    }
}
