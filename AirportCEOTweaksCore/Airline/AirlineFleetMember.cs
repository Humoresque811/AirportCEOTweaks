using System;
using System.Collections.Generic;

namespace AirportCEOTweaksCore;

// Used to be TypeModel (a very unhelpful name)
public class AirlineFleetMember
{
    public AirlineModel ParentAirline { get; private set; }

    public string AircraftName { get; private set; }
    public int NumberInFleet  { get; private set; }

    public AircraftType _AircraftType  { get; private set; }
    public AircraftModel AircraftModel  { get; private set; }
    public int RangeKM  { get; private set; }
    public Enums.GenericSize AircraftSize  { get; private set; }
    private int MaxCapacityPAX { get; set; }

    public bool ErrorFlag  { get; private set; }


    public AirlineFleetMember(AirlineModel parent, string aircraftString, int fleetCount)
    {
        // Setup default values
        ErrorFlag = false;
        RangeKM = 0;
        AircraftSize = Enums.GenericSize.Tiny;
        MaxCapacityPAX = 0;

        AircraftName = aircraftString;
        NumberInFleet = fleetCount;
        ParentAirline = parent;

        if (!CustomEnums.aircraftTypes.ContainsKey(aircraftString))
        {
            AirportCEOTweaksCore.LogWarning($"Aircraft \"{aircraftString}\" is not in directory, and therefore can't be generated. This might be because the user is playing with " +
                $"Tweaks Aircraft but does not have all aircraft packs installed; this warning can be safely ignored if that is the case. Skipping creation of this {typeof(AirlineFleetMember).Name}");
            ErrorFlag = true;
            AircraftModel = null;
            return;
        }

        CustomEnums.TryGetAircraftType(aircraftString, out AircraftType type);
        _AircraftType = type;

        if (Singleton<AirTrafficController>.Instance.GetAircraftModel(_AircraftType.id) == null)
        {
            AirportCEOTweaksCore.LogError($"Failed to fetch aircraft model when making fleet member. String aircraft: \"{aircraftString}\", type created: \"{_AircraftType.id}\""); // This is now an actual issue
            ErrorFlag = true;
            AircraftModel = null;
        }
        else
        {
            AircraftModel = Singleton<AirTrafficController>.Instance.GetAircraftModel(_AircraftType.id);
            RangeKM = AircraftModel.rangeKM;
            AircraftSize = _AircraftType.size;
            MaxCapacityPAX = AircraftModel.MaxPax;
        }
    }

    public bool AvailableByDLC()
    {
        return (!AirTrafficController.IsSupersonic(AircraftName) || DLCManager.OwnsSupersonicDLC) && (!AirTrafficController.IsVintage(AircraftName) || DLCManager.OwnsVintageDLC) && (!AirTrafficController.IsEastern(AircraftName) || DLCManager.OwnsBeastsOfTheEastDLC);
    }

    public bool CanOperateFromOtherAirportSize(Enums.GenericSize airportSize, Enums.GenericSize cargoSize)
    {
        if ((int)AircraftSize > Math.Max((int)airportSize+4,(int)cargoSize+2) || (int)AircraftSize < Math.Min((int)airportSize - 1,(int)cargoSize-2))
        {
            return false;
        }
        return true;
    }

    public bool CanFlyDistance(int distance) //if player does not have fuel service the available route distance is cut in half
    {

        switch (AircraftModel.fuelType)
        {
            case Enums.FuelType.JetA1: if (!Singleton<AirportController>.Instance.hasJetA1FuelDepotWithContent) { distance /= 2; } break;
            case Enums.FuelType.Gasoline:
            case Enums.FuelType.Diesel:
            case Enums.FuelType.Unspecified:
            case Enums.FuelType.Avgas100LL: if (!Singleton<AirportController>.Instance.hasAvgasFuelDepotWithContent) { distance /= 2; } break;
            default: break;
        }

        return (distance < RangeKM);
    }

    public bool CanDispatchAdditionalAircraft()
    {
        return true;
    }

    public bool CanOperateFromPlayerAirportStands(float chanceToOfferRegaurdless)
    {
        switch(AircraftModel.weightClass)
        {
            case Enums.ThreeStepScale.Small: return true;
            case Enums.ThreeStepScale.Medium: if (Singleton<AirportController>.Instance.hasMediumStand || Singleton<AirportController>.Instance.hasLargeStand) { return true; } break;
            case Enums.ThreeStepScale.Large: 
                if (Singleton<AirportController>.Instance.hasLargeStand)
                { return true; } 
                else // dont offer anyways if no medium stand
                { 
                    if (!Singleton<AirportController>.Instance.hasMediumStand) 
                    { return false; } 
                } 
                break;
        }
        if (UnityEngine.Random.Range(0f,1f)<chanceToOfferRegaurdless)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
