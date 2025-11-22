using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AirportCEOTweaksCore;
using KaimiraGames;
using UnityEngine;

namespace AirportCEONationality;

class NationalityFlightGenerator : FlightGeneratorBase
{
    public override string GeneratorName => "Nationality Flight Generator";
    public override bool GetErrorNote(AirlineModel model, out string message)
    {
        if (airlinesAlreadyShownError.Contains(model.businessName))
        {
            message = null;
            return false;
        }

        string messageFilled = $"The ACEO Tweaks {GeneratorName} was unable to generate any realistic flights for {model.businessName}. The airline will now offer flights as per the vanilla game. " +
            $"If you do not want this, consider canceling the contract.";
        AirportCEONationality.LogDebug(messageFilled);
        airlinesAlreadyShownError.Add(model.businessName);

        if (AirportCEONationalityConfig.FallbackGenerationMode.Value != NationalityFallbackRule.FallbackVanillaNotify)
        {
            message = null;
            return false;
        }

        message = messageFilled;
        return true;
    }

    private static List<string> airlinesAlreadyShownError = new();

    private static SortedSet<RouteContainer> routesToSearch = new(); // Just to save the memory, no need to constantly reallocate
    private static WeightedList<RouteContainer> finalRouteOptions = new();

    public static IEnumerator ToggleGeneratorCoroutine()
    {
        ToggleGenerator(AirportCEONationalityConfig.EnableNationalityFlightGeneration.Value);
        yield break;
    }
    public static void ToggleGenerator()
    {
        ToggleGenerator(AirportCEONationalityConfig.EnableNationalityFlightGeneration.Value);
    }
    public static void ToggleGenerator(object _, EventArgs __)
    {
        ToggleGenerator(AirportCEONationalityConfig.EnableNationalityFlightGeneration.Value);
    }
    public static void ToggleGenerator(bool value)
    {
        if (ModsController.Instance == null)
        {
            return;
        }

        if (value)
        {
            ModsController.Instance.flightGenerator = new NationalityFlightGenerator();
            return;
        }

        ModsController.Instance.ResetFlightGenerator();
    }

    public override void GenerateFlightModel(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, out FlightGeneratorResults flightGeneratorResults)
    {
        AirlineModelExtended extendedAirlineModel = airlineModel.ExtendAirlineModel(ref airlineModel);
        FlightGeneratorResultAction failureFallbackGenerationRule = 
            AirportCEONationalityConfig.FallbackGenerationMode.Value == NationalityFallbackRule.DontGenerate ? FlightGeneratorResultAction.DontCreate : FlightGeneratorResultAction.UseVanillaGeneration;

        // Check Possible to Gen a Flight
        if (airlineModel.fleetCount.Length == 0)
        {
            Debug.LogWarning("ACEO Tweaks | WARN: Generate flight for " + airlineModel.businessName + " failed due to FleetCount.Length==0");
            airlineModel.CancelContract();
            Debug.LogWarning("ACEO Tweaks | WARN: Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");

            flightGeneratorResults = new(null, failureFallbackGenerationRule);
            return;
        }
        if (airlineModel.aircraftFleetModels.Length == 0)
        {
            Debug.LogWarning("ACEO Tweaks | WARN: Generate flight for " + airlineModel.businessName + " failed due to FleetModels.Length==0");
            airlineModel.CancelContract();
            Debug.LogWarning("ACEO Tweaks | WARN: Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");

            flightGeneratorResults = new(null, failureFallbackGenerationRule);
            return;
        }


        // Preselect route number ...............................................................................................
        if (!GenerateFlightNumber(airlineModel, out int flightNumber))
        {
            // Failed!
            AirportCEONationality.LogWarning($"Generate flight for \"{airlineModel.businessName}\" failed due to no available flight number!");
            flightGeneratorResults = new(null, FlightGeneratorResultAction.DontCreate);
            return;
        }

        // Main Loop/Loop Prep starts here .......................................................................................................................
        Country[] airlineHomeCountries = extendedAirlineModel.HomeCountries;
        bool remainInHomeCountries = extendedAirlineModel.StayWithinHomeCountries;
        bool playerAirportInHomeCountries = airlineHomeCountries.Contains(GameDataController.GetUpdatedPlayerSessionProfileData().playerAirport.Country);

        // Initial aircraft data loaded
        WeightedList<AirlineFleetMember> fleetMembersWeighted = new();
        FillListWithAirlineFleetInfo(extendedAirlineModel, ref fleetMembersWeighted);

        // Loop iteration variables
        List<CommercialFlightModel> commercialFlightModels = new();
        if (airlineHomeCountries == null || airlineHomeCountries.Length == 0)
        {
            flightGeneratorResults = new(null, failureFallbackGenerationRule);
            return;
        }

        // Main loop!
        while (commercialFlightModels.Count == 0)
        {
            routesToSearch.Clear();
            finalRouteOptions.Clear();

            if (fleetMembersWeighted.Count == 0) // There are no appropriate routes for any member of this airlines fleet based on the settings!
            {
                flightGeneratorResults = new(null, failureFallbackGenerationRule);
                return;
            }

            AirlineFleetMember fleetMemberToUse = fleetMembersWeighted.Next();
            fleetMembersWeighted.Remove(fleetMemberToUse);

            if (!fleetMemberToUse.AvailableByDLC() || !fleetMemberToUse.CanOperateFromPlayerAirportStands(0))
            {
                continue;
            }

            routesToSearch.UnionWith(RouteGenerationController.Instance.RouteContainers);
            if (!playerAirportInHomeCountries) // Add in some big airports from the airlines home country (just in case we dont have any already)
            {
                foreach (Country country in airlineHomeCountries)
                {
                    routesToSearch.UnionWith(RouteGenerationController.Instance.GetRoutesToLargeAirportsInCountry(country));
                }
            }

            foreach (RouteContainer inboundRoute in routesToSearch)
            {
                // To airport is *always* us

                if (inboundRoute.Airport.paxSize.IsSmallerThan(fleetMemberToUse.AircraftSize) || inboundRoute.Distance > fleetMemberToUse.RangeKM) 
                {
                    continue; // We cannot serve a small airport with a big plane, and we cant serve the destination if its too far away
                }

                if (playerAirportInHomeCountries) // This means airline is domestic to players airport
                {
                    if (inboundRoute.VanillaDomestic)
                    {
                        finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, false));
                    }
                    else
                    {
                        if (remainInHomeCountries)
                        {
                            continue;
                        }

                        finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, true));
                    }
                } 
                else
                {
                    bool airportIsInHomeCodes = false;
                    foreach (Country country in airlineHomeCountries)
                    {
                        if (inboundRoute.Airport.Country != country) // Specifically ignoring Schengen here!
                        {
                            continue;
                        }
                        
                        airportIsInHomeCodes = true;
                    }

                    if (!airportIsInHomeCodes)
                    {
                        continue;
                    }


                    finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, true));
                }
            }

            if (finalRouteOptions == null || finalRouteOptions.Count == 0)
            {
                continue;
            }

            RouteContainer route = finalRouteOptions.Next();

            Route inboundRouteF = new Route(route.route);
            Route outboundRouteF = new Route(inboundRouteF);
            outboundRouteF.ReverseRoute();

            if (inboundRouteF == null || outboundRouteF == null)
            {
                AirportCEONationality.LogWarning("Routes generated by NationalityFlightGenerator are null...");
                continue;
            }

            inboundRouteF.routeNbr = flightNumber;
            outboundRouteF.routeNbr = flightNumber;

            int numInSeries = isEmergency ? 1 : Utils.RandomRangeI(2f, 5f);
            if (!isEmergency)
            {
			    numInSeries = numInSeries.ClampMax(SingletonNonDestroy<BusinessController>.Instance.GetMaxActiveFlights(airlineModel.rating) - extendedAirlineModel.ActiveCount);
		    }

            for (int i = 0; i < numInSeries; i++)
            {
                CommercialFlightModel commercialFlightModel = new CommercialFlightModel(airlineModel.referenceID, true, fleetMemberToUse._AircraftType.id, inboundRouteF, outboundRouteF);
                commercialFlightModel.isEmergency = isEmergency;
                commercialFlightModel.numberOfFlightsInSerie = numInSeries;

			    if (isEmergency)
			    {
				    commercialFlightModel.ResetDeparingPassengers();
			    }
			    if (isAmbulance)
			    {
				    commercialFlightModel.ResetArrivingPassengers();
				    commercialFlightModel.isAmbulance = true;
			    }
                commercialFlightModels.Add(commercialFlightModel);
            }
        }

        if (commercialFlightModels.Count > 0)
        {
            flightGeneratorResults = new(commercialFlightModels, FlightGeneratorResultAction.AllocateFlights);
            return;
        }

        flightGeneratorResults = new(null, failureFallbackGenerationRule);
        return;
    }

    private static void FillListWithAirlineFleetInfo(AirlineModelExtended extendedAirlineModel, ref WeightedList<AirlineFleetMember> fleetMembersWeighted)
    {
        fleetMembersWeighted.Clear();
        foreach (AirlineFleetMember fleetMember in extendedAirlineModel.AirlineFleetMembers)
        {
            fleetMembersWeighted.Add(fleetMember, fleetMember.NumberInFleet);
        }
    }

    private static bool GenerateFlightNumber(AirlineModel airlineModel, out int flightNumber)
    {
        int maxFlightNumber = (((int)airlineModel.businessClass + 3) ^ 2) * 50 + Utils.RandomRangeI(100f, 200f);
        flightNumber = Utils.RandomRangeI(1f, maxFlightNumber);

        // duplicate checking
        for (int i = 0; ; i++)
        {
            if (Singleton<ModsController>.Instance.FlightsByFlightNumber(airlineModel, airlineModel.airlineFlightNbr + flightNumber).Count > 0)
            {
                flightNumber = Utils.RandomRangeI(1f, maxFlightNumber);
                if (i > 200)
                {
                    return false;
                }
            }
            else
            {
                break;
            }
        } 

        return true;
    }

    public bool FleetMemberCanServeRoute(AirlineFleetMember fleetMember, RouteContainer route, float chanceToOfferRegaurdless = 0, bool debug = false)
    {
        if (debug)
        {
            if (!fleetMember.AvailableByDLC()) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftName + " not available by DLC"); }
            if (!fleetMember.CanOperateFromOtherAirportSize(route.Airport.paxSize, route.Airport.cargoSize)) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftName + " cannot operate from " + route.Airport.airportName + " ["+ route.Airport.airportIATACode + "]"); }
            if (!fleetMember.CanFlyDistance(route.Distance.RoundToIntLikeANormalPerson())) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftName + " cannot fly distance " + route.Distance + "km (do you have refueling available?)"); }
            if (!fleetMember.CanDispatchAdditionalAircraft()) { }
            if (!fleetMember.CanOperateFromPlayerAirportStands(0)) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftName + " cannot operate from player stand sizes"); }
        }
            
        return
            fleetMember.AvailableByDLC() &&
            fleetMember.CanOperateFromOtherAirportSize(route.Airport.paxSize , route.Airport.cargoSize) &&
            fleetMember.CanFlyDistance(route.Distance.RoundToIntLikeANormalPerson()) &&
            fleetMember.CanDispatchAdditionalAircraft() &&
            fleetMember.CanOperateFromPlayerAirportStands(chanceToOfferRegaurdless);
    }

    public int SuitabilityForRoute(AirlineFleetMember fleetMember, RouteContainer routeThatIsPossible, bool isInternational)
    {
        float suitability = 1000;

        suitability *= GetRangeSuitabilityModifier(routeThatIsPossible.Distance / fleetMember.RangeKM, fleetMember.AircraftModel.weightClass);
        suitability *= GetSizeMismatchSuitabilityModifier(routeThatIsPossible.Airport.paxSize, fleetMember.AircraftSize, isInternational);
        suitability *= GetEasternModifier(fleetMember, routeThatIsPossible);
        suitability *= GetVintageModifier(fleetMember);
        suitability *= GetInternationalModifier(routeThatIsPossible.Airport.paxSize, fleetMember.AircraftSize, isInternational);

        return suitability.RoundToIntLikeANormalPerson();
    }

    private float GetRangeSuitabilityModifier(float rangeUtilization, Enums.ThreeStepScale weightClass)
    {
        // Don't understand the math? Look at Desmos for graphs: https://www.desmos.com/calculator/jpzbskk7ei
        if (weightClass == Enums.ThreeStepScale.Small)
        {
            return 0.2f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.5f, 2)) + 0.8f;
        }
        else if (weightClass == Enums.ThreeStepScale.Medium)
        {
            return 0.2f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.8f, 2)) + 0.8f;
        }
        else
        {
            return 0.4f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.8f, 2)) + 0.6f;
        }
    }
    private float GetSizeMismatchSuitabilityModifier(Enums.GenericSize airportSize, Enums.GenericSize flightSize, bool isInternational)
    {
        int difference = Math.Abs(airportSize - flightSize);

        // Don't understand the math? Look at Desmos for graphs: https://www.desmos.com/calculator/jpzbskk7ei
        if (!isInternational)
        {
            return -1f / (1 + Mathf.Pow(3, -1f * difference + 3.5f)) + 1;
        }
        else
        {
            return -1f / (1 + Mathf.Pow(3, -1.5f * difference + 3.5f)) + 1;
        }
    }

    private float GetEasternModifier(AirlineFleetMember fleetMember, RouteContainer route)
    {
        // More likely from former USSR!
        float suitability = 1f;
        if (AirTrafficController.IsEastern(fleetMember.AircraftName) || fleetMember._AircraftType.id == "TU144")
        {
            bool ussr = false;
            string[] codes = new string[] {"AM","AZ","BY","EE","GE","KZ","KG","LV","LT","MD","RU","TJ","TM","UA","UZ"};
            foreach(string code in codes)
            {
                ussr = code == route.country.countryCode ? true : ussr;
                ussr = code == GameDataController.GetUpdatedPlayerSessionProfileData().playerAirport.Country.countryCode ? true : ussr;
                if (ussr) { break; }
            }

            if (!ussr)
            {
                suitability = 0.5f;
            }
        }  
        return suitability;
    }

    private float GetVintageModifier(AirlineFleetMember fleetMember)
    {
        bool isVintage = AirTrafficController.IsVintage(fleetMember.AircraftName);

        if (AirportCEONationalityConfig.VintageGenerationMultiplier.Value <= 1)
        {
            return isVintage ? AirportCEONationalityConfig.VintageGenerationMultiplier.Value : 1f;
        }

        return isVintage ? 1f / AirportCEONationalityConfig.VintageGenerationMultiplier.Value : 1f;
    }

    private float GetInternationalModifier(Enums.GenericSize airportSize, Enums.GenericSize flightSize, bool isInternational)
    {
        if (!isInternational)
        {
            return 1;
        }
        if (airportSize.IsSmallerThan(Enums.GenericSize.Large) || flightSize.IsSmallerThan(Enums.GenericSize.Medium))
        {
            return 0.75f;
        }

        return 1;
    }

    public int SuitabilityForRoute(AirlineFleetMember fleetMember, RouteContainer routeThatIsPossible, bool isInternational, bool forceCargo = false)
    {
        float rangecap;
        bool cargo = fleetMember.AircraftModel.maxPax == 0 ? true : forceCargo;
        int airportSize = cargo ? (int)routeThatIsPossible.Airport.cargoSize : (int)routeThatIsPossible.Airport.paxSize;

        switch(fleetMember.AircraftModel.seatRows)
        {
            case 1:
            case 2:
            case 3: rangecap = .2f; break;
            case 4:
            case 5:
            case 6: rangecap = .4f; break;
            case 7: rangecap = .7f; break;
            case 8: rangecap = .7f; break;
            default: rangecap = .8f; break;
        }

        if (cargo) rangecap = 0.5f;
            
        int sizeMismatch = airportSize - (int)fleetMember.AircraftSize; // 0,1,2,3,4...
        if (sizeMismatch > 0)
        {
            sizeMismatch -= 1; // Allow for smaller planes to go to bigger airports with less penalty
        }
        sizeMismatch = Math.Abs(sizeMismatch);
        sizeMismatch = sizeMismatch == 0 ? 1 : sizeMismatch;                                         // 1,1,2,3,4...

        float rangeUtilization = ((routeThatIsPossible.Distance/fleetMember.RangeKM).Clamp(0f,rangecap))/rangecap; //utilizing range is good, where possible shorter range aircraft should be used for shorter routes.
            

        float suitability = (rangeUtilization*100) / sizeMismatch;
        //suitability = (float)(suitability * fleetMember.NumberInFleet); // Already accounted for in the generation

        // Post-processing special conditions

        if (AirTrafficController.IsSupersonic(fleetMember.AircraftName))
        {
            if (routeThatIsPossible.Etops)
            {
                suitability *= 2;
            }
            else
            {
                suitability /= 2;
            }
        }                             //more likely for ocean crossing
        if (AirTrafficController.IsEastern(fleetMember.AircraftName) || fleetMember._AircraftType.id == "TU144")
        {
            bool ussr = false;
            string[] codes = new string[] {"AM","AZ","BY","EE","GE","KZ","KG","LV","LT","MD","RU","TJ","TM","UA","UZ"};
            foreach(string code in codes)
            {
                ussr = code == routeThatIsPossible.country.countryCode ? true : ussr;
                ussr = code == GameDataController.GetUpdatedPlayerSessionProfileData().playerAirport.Country.countryCode ? true : ussr;
                if (ussr) { break; }
            }

            if (ussr)
            {
                suitability *= 2;
            }
            else
            {
                suitability /= 2;
            }
        }  //more likely from former USSR
        if (AirTrafficController.IsVintage(fleetMember.AircraftName))
        {
            suitability /= 3;
        }                                //less likely

        if (suitability == float.NaN)
        {
            Debug.LogError("ACEO Tweaks | ERROR: Route Suitability is NaN! Info: aircraft = " + fleetMember.AircraftName + ", range utilization = " + rangeUtilization + "sizeMismatch = " + sizeMismatch);
            return 0;
        }

        if (isInternational)
        {
            // Reduce chance that smaller flights generate across borders
            suitability *= fleetMember.AircraftSize < Enums.GenericSize.Medium ? 0.75f : 1f;
            suitability *= airportSize < (int)Enums.GenericSize.Medium ? 0.75f : 1f;
        }

        return Utils.RoundToIntLikeANormalPerson((suitability + UnityEngine.Random.Range(-0.2f*suitability,0.2f*suitability)) * 1000);
    }    
}
