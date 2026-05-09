using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportCEOTweaksCore;

public struct FlightGeneratorResults(List<CommercialFlightModel> commercialFlightModels, FlightGeneratorResultAction action)
{
    // Wow fancy modern c# syntax
    public List<CommercialFlightModel> commercialFlightModels = commercialFlightModels;
    public FlightGeneratorResultAction action = action;
}

public enum FlightGeneratorResultAction
{
    // This means that the flight generator ...
    AllocateFlights,                    //  has successfully generated flight models, and they should be allocated as normal
    UseVanillaGeneration,               //  failed to generate flight models, but the vanilla generation should be used instead (this is different from DontCreate, which means that no flight should be created at all)
    DontCreate,                         //  failed to generate flight models, and the vanilla generation should NOT be used, so no flight should be created at all
    AlreadyAllocated,                   //  has already allocated the flights itself, so no further action should be taken (this is different from AllocateFlights, which means that the flight generator has generated flight models but has not allocated them yet)
}