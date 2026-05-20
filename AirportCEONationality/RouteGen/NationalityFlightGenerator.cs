using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AirportCEOTweaksCore;
using KaimiraGames;
using UnityEngine;
using System.Text;
using AirportCEOModLoader.Core;

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

        bool isVanilla = false;
        if (model is AirlineModelExtended extendedModel)
        {
            isVanilla = !extendedModel.IsCustom;
        }

        if (isVanilla && hasShownVanillaError)
        {
            message = null;
            return false;
        }
        else if (isVanilla)
        {
            hasShownVanillaError = true;
        }

        string submessage = GenerateSubMessage(AirportCEONationalityConfig.FallbackGenerationMode.Value);

        string messageFilled;
        if (isVanilla)
        {
            messageFilled = $"The ACEO Tweaks {GeneratorName} is unable to generate any realistic flights for all vanilla airlines (for obvious reasons). The airline will {submessage}. " +
                $"If you do not want this, consider canceling the contract.";
        }
        else
        {
            messageFilled = $"The ACEO Tweaks {GeneratorName} was unable to generate any realistic flights for {model.businessName}{GenerateHomeCountryError(model.businessName)}. " +
                $"The airline will {submessage}. If you do not want this, consider canceling the contract.";
        }

        AirportCEONationality.LogError(messageFilled);
        airlinesAlreadyShownError.Add(model.businessName);

        if (!AirportCEONationalityConfig.ShowWarningWhenFallingBack.Value)
        {
            message = null;
            return false;
        }

        message = messageFilled;
        return true;
    }

    private static string GenerateSubMessage(NationalityFallbackRule rule)
    {
        switch (rule)
        {
            case NationalityFallbackRule.FallbackVanilla:
                return "now generate flights as per the vanilla game";
            case NationalityFallbackRule.DontGenerate:
                return "not generate any flights";
            default:
                return "do something (if you see this humor made an oopsie. Please tell me ;) )";
        }
    }

    private static string GenerateHomeCountryError(string airlineName)
    {
        if (!airlinesWithHomeCountryErrors.Contains(airlineName))
        {
            return "";
        }

        return " becuase it lacked or had an incorrect country code";
    }

    private static List<string> airlinesAlreadyShownError = new();
    private static List<string> airlinesAlreadyShownLimitError = new();
    private static List<string> airlinesWithHomeCountryErrors = new();
    private static bool hasShownVanillaError = false;

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
        GenerateFlightModelInternal(airlineModel, isEmergency, isAmbulance, false, out FlightGeneratorResults result);

        if (result.action == FlightGeneratorAction.AllocateFlights || result.action == FlightGeneratorAction.AlreadyAllocated)
        {
            flightGeneratorResults = result;
            return;
        }

        if (!AirportCEONationalityConfig.IgnoreRangeLimitsFirst.Value)
        {
            flightGeneratorResults = result;
            return;
        }

        // Now testing ignoring range limits
        GenerateFlightModelInternal(airlineModel, isEmergency, isAmbulance, true, out FlightGeneratorResults resultIgnoreRange);
        if (resultIgnoreRange.action == FlightGeneratorAction.AllocateFlights || resultIgnoreRange.action == FlightGeneratorAction.AlreadyAllocated)
        {
            flightGeneratorResults = resultIgnoreRange;
            flightGeneratorResults.shouldShowMessage = false; // We show this message ourselves below
            if (airlinesAlreadyShownLimitError.Contains(airlineModel.businessName))
            {
                return;
            }

            // Show message about range limits being ignored, but only once per airline to avoid spam
            string message = $"The ACEO Tweaks {GeneratorName} was unable to generate any realistic flights for {airlineModel.businessName}, however it could ignoring " +
            $"range limits. The airline will now generate flights without range limits.";

            AirportCEONationality.LogInfo(message);
            DialogUtils.QueueDialog(message);

            airlinesAlreadyShownLimitError.Add(airlineModel.businessName);
            return;
        }

        flightGeneratorResults = resultIgnoreRange;
        return;
    }

    public void GenerateFlightModelInternal(AirlineModel airlineModel, bool isEmergency, bool isAmbulance, bool ignoreRangeLimits, out FlightGeneratorResults flightGeneratorResults)
    {
        if (AirportCEONationalityConfig.ExtraDebugLogs.Value)
        {
            AirportCEONationality.LogDebug($"Starting generation of flight for airline \"{airlineModel.businessName}\" with ignoreRangeLimits={ignoreRangeLimits}");
        }

        AirlineModelExtended extendedAirlineModel = airlineModel.ExtendAirlineModel(ref airlineModel);
        FlightGeneratorAction failureFallbackGenerationRule = 
            AirportCEONationalityConfig.FallbackGenerationMode.Value == NationalityFallbackRule.DontGenerate ? FlightGeneratorAction.DontCreate : FlightGeneratorAction.UseVanillaGeneration;

        // Check Possible to Gen a Flight
        if (airlineModel.fleetCount.Length == 0)
        {
            AirportCEONationality.LogWarning("Generate flight for " + airlineModel.businessName + " failed due to FleetCount.Length==0");
            airlineModel.CancelContract();
            AirportCEONationality.LogWarning("Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");

            flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
            return;
        }
        if (airlineModel.aircraftFleetModels.Length == 0)
        {
            AirportCEONationality.LogWarning("Generate flight for " + airlineModel.businessName + " failed due to FleetModels.Length==0");
            airlineModel.CancelContract();
            AirportCEONationality.LogWarning("Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");

            flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
            return;
        }


        // Preselect route number ...............................................................................................
        if (!GenerateFlightNumber(airlineModel, out int flightNumber))
        {
            // Failed!
            AirportCEONationality.LogWarning($"Generate flight for \"{airlineModel.businessName}\" failed due to no available flight number!");
            flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
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
            if (!airlinesAlreadyShownError.Contains(airlineModel.businessName))
            {
                AirportCEONationality.LogWarning($"Generate flight for \"{airlineModel.businessName}\" failed due to no home countries!");
            }
            airlinesWithHomeCountryErrors.Add(airlineModel.businessName);
            flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
            return;
        }

        RouteContainer DEBUG_routeContainer = null;
        AirlineFleetMember DEBUG_fleetMember = null;

        if (AirportCEONationalityConfig.ExtraDebugLogs.Value)
        {
            AirlineFleetMember maxRange = fleetMembersWeighted.OrderByDescending(x => x.RangeKM).FirstOrDefault();
            Airport closestAirportInHomeCountries = null;
            foreach (Country country in airlineHomeCountries)
            {
                foreach (Airport airport in RouteGenerationController.Instance.GetAirportsInCountry(country))
                {
                    if (closestAirportInHomeCountries == null || RouteGenerationController.GetDistanceToAirport(airport) < RouteGenerationController.GetDistanceToAirport(closestAirportInHomeCountries))
                    {
                        closestAirportInHomeCountries = airport;
                    }
                }
            }
            AirportCEONationality.LogDebug($"Pre-main loop debug info for airline \"{airlineModel.businessName}\" [T-N Log ID 2]:\nPlayer airport in {RouteGenerationController.PlayerAirport.CountryName}, " +
                $"Airline home countries={PrintEnumerable(airlineHomeCountries, c => { return c.countryName; })}, fleet member with max range is {maxRange.AircraftName} with range {maxRange.RangeKM}km, " +
                $"closest airport in home countries is {closestAirportInHomeCountries.airportName} with distance {(int)RouteGenerationController.GetDistanceToAirport(closestAirportInHomeCountries)}km");
        }

        // Main loop!
        while (commercialFlightModels.Count == 0)
        {
            routesToSearch.Clear();
            finalRouteOptions.Clear();

            if (fleetMembersWeighted.Count == 0) // There are no appropriate routes for any member of this airlines fleet based on the settings!
            {
                flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
                return;
            }

            AirlineFleetMember fleetMemberToUse = fleetMembersWeighted.Next();
            fleetMembersWeighted.Remove(fleetMemberToUse);

            if (!fleetMemberToUse.AvailableByDLC() || !fleetMemberToUse.CanOperateFromPlayerAirportStands(0))
            {
                continue;
            }

            routesToSearch.UnionWith(RouteGenerationController.Instance.RouteContainers);
            routesToSearch.UnionWith(RouteGenerationController.Instance.GetDomesticAirports());
            routesToSearch.UnionWith(RouteGenerationController.Instance.GetNearAirports());
            if (!playerAirportInHomeCountries) // Add in some big airports from the airlines home country (just in case we dont have any already)
            {
                foreach (Country country in airlineHomeCountries)
                {
                    routesToSearch.UnionWith(RouteGenerationController.Instance.GetRoutesToLargerAirportsInCountry(country));
                }
            }

            foreach (RouteContainer inboundRoute in routesToSearch)
            {
                // To airport is *always* us

                if (inboundRoute.Airport.paxSize.IsSmallerThan(fleetMemberToUse.AircraftSize + 2)) // We allow bigger planes to smaller airports  
                {
                    continue; // We cannot serve a small airport with a big plane
                }

                if (inboundRoute.Distance > fleetMemberToUse.RangeKM && !ignoreRangeLimits) 
                {
                    continue; // We cant serve the destination if its too far away
                }

                if (playerAirportInHomeCountries) // This means airline is domestic to players airport
                {
                    if (inboundRoute.VanillaDomestic)
                    {
                        finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, false, ignoreRangeLimits));
                    }
                    else
                    {
                        if (remainInHomeCountries)
                        {
                            continue;
                        }

                        finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, true, ignoreRangeLimits));
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


                    finalRouteOptions.Add(inboundRoute, SuitabilityForRoute(fleetMemberToUse, inboundRoute, true, ignoreRangeLimits));
                }
            }

            if (finalRouteOptions == null || finalRouteOptions.Count == 0)
            {
                continue;
            }

            RouteContainer route = finalRouteOptions.Next();
            DEBUG_routeContainer = route;
            DEBUG_fleetMember = fleetMemberToUse;

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
            flightGeneratorResults = new(commercialFlightModels, FlightGeneratorAction.AllocateFlights, false);
            if (AirportCEONationalityConfig.ExtraDebugLogs.Value)
            {
                PrintDebugInfo(airlineModel, finalRouteOptions, DEBUG_routeContainer, DEBUG_fleetMember);
            }
            return;
        }

        flightGeneratorResults = new(null, failureFallbackGenerationRule, true);
        return;
    }

    private static string PrintEnumerable<T>(IEnumerable<T> collection, Func<T, string> processor)
    {
        if (collection == null || collection.Count() == 0)
        {
            return "{}";
        }

        StringBuilder builder = new();
        builder.Append("{");
        foreach (T item in collection)
        {
            builder.Append(processor(item));
            builder.Append(", ");
        }
        builder.Length -= 2; // Remove last comma and space
        builder.Append("}");
        return builder.ToString();
    }

    private static void PrintDebugInfo(AirlineModel model, WeightedList<RouteContainer> routes, RouteContainer flight, AirlineFleetMember member)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Debug info for the generation of flights for airline \"{model.businessName}\", aircraft \"{member.AircraftName}\" (size {member.AircraftSize}, range {member.RangeKM}) [T-N Log ID 1]: ");
        builder.AppendLine($"Chosen: Flight to {flight.Airport.airportName} size {flight.Airport.paxSize}, distance {flight.Distance}, with weight {routes.GetWeightOf(flight)}");
        
        builder.AppendLine("");
        builder.AppendLine("All options & weights:");

        for (int i = 0; i < routes.Count; i++)
        {
            builder.AppendLine($"Flight to {routes[i].Airport.airportName} country {routes[i].Airport.CountryName} size {routes[i].Airport.paxSize}, distance {routes[i].Distance}, with weight {routes.GetWeightAtIndex(i)}");
        }

        builder.AppendLine("End of info - - - - - - - - - - - - - - - - - - -");
        AirportCEONationality.LogDebug(builder.ToString());
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


    public int SuitabilityForRoute(AirlineFleetMember fleetMember, RouteContainer routeThatIsPossible, bool isInternational, bool ignoreRangeLimits)
    {
        float suitability = 1000;

        suitability *= GetRangeSuitabilityModifier(routeThatIsPossible.Distance / fleetMember.RangeKM, fleetMember.AircraftModel.weightClass, ignoreRangeLimits);
        suitability *= GetSizeMismatchSuitabilityModifier(routeThatIsPossible.Airport.paxSize, fleetMember.AircraftSize, isInternational);
        suitability *= GetEasternModifier(fleetMember, routeThatIsPossible);
        suitability *= GetVintageModifier(fleetMember);
        suitability *= GetInternationalModifier(routeThatIsPossible.Airport.paxSize, fleetMember.AircraftSize, isInternational);

        return suitability.RoundToIntLikeANormalPerson();
    }

    private float GetRangeSuitabilityModifier(float rangeUtilization, Enums.ThreeStepScale weightClass, bool ignoreRangeLimits)
    {
        float currentModifier = 0;

        // Don't understand the math? Look at Desmos for graphs: https://www.desmos.com/calculator/jpzbskk7ei
        if (weightClass == Enums.ThreeStepScale.Small)
        {
            currentModifier = 0.3f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.5f, 2)) + 0.7f;
        }
        else if (weightClass == Enums.ThreeStepScale.Medium)
        {
            currentModifier = 0.3f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.8f, 2)) + 0.7f;
        }
        else
        {
            currentModifier = 0.5f * Mathf.Pow(2, -20 * Mathf.Pow(rangeUtilization - 0.8f, 2)) + 0.5f;
        }

        currentModifier = Mathf.Min(currentModifier, 4 * rangeUtilization);

        if (!ignoreRangeLimits || rangeUtilization <= 1)
        {
            return currentModifier;
        }

        // Ignoring range limits - // Don't understand the math? Look at Desmos for graphs: https://www.desmos.com/calculator/jpzbskk7ei
        return Mathf.Max(3 * Mathf.Pow(.2f, rangeUtilization), 0.05f); 

    }
    private float GetSizeMismatchSuitabilityModifier(Enums.GenericSize airportSize, Enums.GenericSize flightSize, bool isInternational)
    {
        int difference = Math.Abs(airportSize - flightSize);

        // Don't understand the math? Look at Desmos for graphs: https://www.desmos.com/calculator/jpzbskk7ei
        if (airportSize.IsSmallerThan(flightSize))
        {
            return Mathf.Max(0, ((flightSize - airportSize) / 4f) + .75f);
        }

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
        float returnMultiplier;
        if (AirportCEONationalityConfig.InternationalGenerationMultiplier.Value >= 1)
        {
            returnMultiplier = isInternational ? 1 : 1f / AirportCEONationalityConfig.InternationalGenerationMultiplier.Value;
        }
        else
        {
            returnMultiplier = isInternational ? AirportCEONationalityConfig.InternationalGenerationMultiplier.Value : 1f;
        }

        if (isInternational && (airportSize.IsSmallerThan(Enums.GenericSize.Large) || flightSize.IsSmallerThan(Enums.GenericSize.Medium)))
        {
            returnMultiplier *= 0.75f;
        }

        return returnMultiplier;
    }    
}
