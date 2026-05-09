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

    public override void GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out FlightGeneratorResults flightGeneratorResults)
    {
        //return airlineModel.ExtendAirlineModel(ref airlineModel).GenerateFlight(isEmergency, isAmbulance);
        Debug.Log("DefaultFlightGeneratorGenerateFlight00");

        OverrideHarmonyPrefix = true;
        bool success = airlineModel.GenerateFlight(isEmergency, isAmbulance);
        OverrideHarmonyPrefix = false;

        flightGeneratorResults = new FlightGeneratorResults(null, FlightGeneratorResultAction.AlreadyAllocated);
    }
}
