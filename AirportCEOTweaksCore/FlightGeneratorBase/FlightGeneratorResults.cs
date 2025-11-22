using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportCEOTweaksCore;

public struct FlightGeneratorResults
{
    public List<CommercialFlightModel> commercialFlightModels;
    public FlightGeneratorResultAction action;

    public FlightGeneratorResults(List<CommercialFlightModel> models, FlightGeneratorResultAction action)
    {
        this.commercialFlightModels = models;
        this.action = action;
    }
}

public enum FlightGeneratorResultAction
{
    AllocateFlights,
    UseVanillaGeneration,
    DontCreate,
    AlreadyAllocated,
}