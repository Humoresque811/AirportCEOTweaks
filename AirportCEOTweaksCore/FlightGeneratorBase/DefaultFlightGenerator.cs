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
        OverrideHarmonyPrefix = true;
        bool _ = airlineModel.GenerateFlight(isEmergency, isAmbulance);
        OverrideHarmonyPrefix = false;

        flightGeneratorResults = new FlightGeneratorResults(null, FlightGeneratorAction.AlreadyAllocated, false);
    }
}
