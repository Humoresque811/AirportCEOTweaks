using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirportCEOTweaksCore;
using UnityEngine;
using static Enums;
using UnityEngine.SocialPlatforms;

namespace AirportCEONationality
{
    class NationalityFlightGenerator : IFlightGenerator
    {
        public bool OverrideHarmonyPrefix => false;

        public bool GenerateFlight(AirlineModel airlineModel, bool isEmergency, bool isAmbulance)
        {
            AirlineModelExtended extendedAirlineModel = airlineModel.ExtendAirlineModel(ref airlineModel);

            // Check Possible to Gen a Flight
            if (airlineModel.fleetCount.Length == 0)
            {
                Debug.LogWarning("ACEO Tweaks | WARN: Generate flight for " + airlineModel.businessName + " failed due to FleetCount.Length==0");
                airlineModel.CancelContract();
                Debug.LogWarning("ACEO Tweaks | WARN: Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");
                return true;
            }
            if (airlineModel.aircraftFleetModels.Length == 0)
            {
                Debug.LogWarning("ACEO Tweaks | WARN: Generate flight for " + airlineModel.businessName + " failed due to FleetModels.Length==0");
                airlineModel.CancelContract();
                Debug.LogWarning("ACEO Tweaks | WARN: Airline " + airlineModel.businessName + "contract canceled due to no valid fleet!");
                return true;
            }


            // Preselect route number ...............................................................................................

            int maxFlightNumber = (((int)airlineModel.businessClass + 3) ^ 2) * 50 + Utils.RandomRangeI(100f, 200f);
            int flightNumber = Utils.RandomRangeI(1f, maxFlightNumber);

            // duplicate checking
            for (int i = 0; ; i++)
            {
                if (Singleton<ModsController>.Instance.FlightsByFlightNumber(airlineModel, airlineModel.airlineFlightNbr + flightNumber).Count > 0)
                {
                    flightNumber = Utils.RandomRangeI(1f, maxFlightNumber);
                    if (i > 200)
                    {
                        Debug.LogWarning("ACEO Tweaks | WARN: Generate flight for " + airlineModel.businessName + " failed due to no available flight number");
                        return false;
                    }
                }
                else
                {
                    break;
                }
            }


            // Get data for decision
            float maxRange = 0;
            float minRange = float.MaxValue;
            float desiredRange;
            Country[] airlineHomeCountries = extendedAirlineModel.HomeCountries;
            bool remainInHomeCountries = extendedAirlineModel.airlineBusinessData.remainWithinHomeCodes;
            bool playerAirportInHomeCountries = airlineHomeCountries.Contains(GameDataController.GetUpdatedPlayerSessionProfileData().playerAirport.Country);


            // Select Aircraft - We start with this and then find a route for it to fly
            AircraftModel selectedAircraft = null;
            AircraftType selectedAircraftType = default;

            

            //Select Route

            //Instantiate the Flights
        }

        public bool FleetMemberCanServeRoute(AirlineFleetMember fleetMember, RouteContainer route, float chanceToOfferRegaurdless = 0, bool debug = false)
        {
            if (debug)
            {
                if (!fleetMember.AvailableByDLC()) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftString + " not available by DLC"); }
                if (!fleetMember.CanOperateFromOtherAirportSize(route.Airport.paxSize, route.Airport.cargoSize)) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftString + " cannot operate from " + route.Airport.airportName + " ["+ route.Airport.airportIATACode + "]"); }
                if (!fleetMember.CanFlyDistance(route.Distance.RoundToIntLikeANormalPerson())) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftString + " cannot fly distance " + route.Distance + "km (do you have refueling available?)"); }
                if (!fleetMember.CanDispatchAdditionalAircraft()) { }
                if (!fleetMember.CanOperateFromPlayerAirportStands(0)) { Debug.Log("ACEO Tweaks | Info: " + fleetMember.AircraftString + " cannot operate from player stand sizes"); }
            }
                
            return
                fleetMember.AvailableByDLC() &&
                fleetMember.CanOperateFromOtherAirportSize(route.Airport.paxSize , route.Airport.cargoSize) &&
                fleetMember.CanFlyDistance(route.Distance.RoundToIntLikeANormalPerson()) &&
                fleetMember.CanDispatchAdditionalAircraft() &&
                fleetMember.CanOperateFromPlayerAirportStands(chanceToOfferRegaurdless);
        }
        public float SuitabilityForRoute(AirlineFleetMember fleetMember, RouteContainer routeThatIsPossible, bool forceCargo = false)
        {
            float rangecap = 1f;
            bool cargo = fleetMember.AircraftModel.maxPax == 0 ? true : forceCargo;
            int size = cargo ? (int)routeThatIsPossible.Airport.cargoSize : (int)routeThatIsPossible.Airport.paxSize;

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
                
            int sizeMismatch = (Math.Abs(size - (int)fleetMember.AircraftSize)); // 0,1,2,3,4...
            sizeMismatch = sizeMismatch == 0 ? 1 : sizeMismatch;                                         // 1,1,2,3,4...

            float rangeUtilization = ((routeThatIsPossible.Distance/fleetMember.RangeKM).Clamp(0f,rangecap))/rangecap; //utilizing range is good, where possible shorter range aircraft should be used for shorter routes.
                

            float suitability = (rangeUtilization*100) / sizeMismatch;
            suitability = (float)(suitability * fleetMember.FleetCount);

            // Post-processing special conditions

            if (AirTrafficController.IsSupersonic(fleetMember.AircraftString))
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
            if (AirTrafficController.IsEastern(fleetMember.AircraftString) || fleetMember._AircraftType.id == "TU144")
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
            if (AirTrafficController.IsVintage(fleetMember.AircraftString))
            {
                suitability /= 3;
            }                                //less likely

            if (suitability == float.NaN)
            {
                Debug.LogError("ACEO Tweaks | ERROR: Route Suitibility is NaN! Info: aircraft = " + fleetMember.AircraftString + ", range utilization = " + rangeUtilization + "sizeMismatch = " + sizeMismatch);
                return 0f;
            }
            return (suitability + UnityEngine.Random.Range(-0.2f*suitability,0.2f*suitability));
        }    
    }
}
