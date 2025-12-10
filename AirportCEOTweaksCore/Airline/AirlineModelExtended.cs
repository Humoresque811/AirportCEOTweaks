using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Unity;
using HarmonyLib.Tools;
using System.Reflection;
using AirportCEOModLoader.Core;
using TMPro;

namespace AirportCEOTweaksCore
{
    public class AirlineModelExtended : AirlineModel
    {
        // This block of props are always filled in! They can always safely be used to get the correct information
        private AirlineBusinessData airlineBusinessData { get; set; }
        public AirlineModel ParentModel { get; private set; }
        public Country[] HomeCountries { get; private set; }
        public List<AirlineFleetMember> AirlineFleetMembers { get; private set; }
        //public string[] FleetModels { get; private set; }
        //public int[] FleetCounts { get; private set; }
        public int TotalFleetCount
        {
            get
            {
                int total = 0;
                foreach (int fleetMemberCount in fleetCount)
                {
                    total += fleetMemberCount;
                }
                return total;
            }
        }
        public bool StayWithinHomeCountries => airlineBusinessData.remainWithinHomeCodes;

        private bool _shouldGenerateFleet = true;

        //public Enums.BusinessClass starRank;
        //public float economyTier = 2;
        //public HashSet<PAXCommercialFlightModelExtended> myFlights;
        //public float cargoProportion = 0f;
        //public float maxRange = 0f;
        //public float minRange = float.MaxValue;
        //private string countryCode;
        //public Country[] forbidCountries;
        //public Dictionary<Airport,float> hUBs;
        //private List<AirlineModel> brandOrAlliance;
        //private List<AirlineModel> siblings;
        //private List<AirlineModel> parents;

        //private Dictionary<string, (float Available, float Allocated)> AircraftTypeAllocation = new Dictionary<string, (float Available, float Allocated)>();

        public AirlineModelExtended(Airline airline, ref AirlineModel airlineModel) : base(airline)
        {
            if (airline == null)
            {
                Debug.LogError("ERROR: Airline Model Extended ctor encountered airline == null!");
                return;
            }
            if (Singleton<ModsController>.Instance == null)
            {
                Debug.LogError("ERROR: Airline Model Extended ctor encountered ModsController == null!");
                return;
            }

            ParentModel = airlineModel;

            Singleton<BusinessController>.Instance.RemoveFromBusinessList(this);
            ConsumeBaseAirlineModel(airlineModel);

            if (Singleton<ModsController>.Instance.airlineBusinessDataByBusinessName.TryGetValue(businessName, out AirlineBusinessData data))
            {
                airlineBusinessData = data;
            }
            else
            {
                Debug.LogWarning("ACEO Tweaks WARN: No airlinebusinessdata path for " + businessName);
            }

            airlineModel = this;
            Singleton<BusinessController>.Instance.RemoveFromBusinessList(this);
            Singleton<BusinessController>.Instance.AddToBusinessList(this);

            MakeUpdateFleet();
            if (airlineBusinessData.arrayHomeCountryCodes == null || airlineBusinessData.arrayHomeCountryCodes.Length == 0)
            {
                HomeCountries = CountryRetriever([airline.countryCode]);
            }
            else
            {
                HomeCountries = CountryRetriever(airlineBusinessData.arrayHomeCountryCodes);
            }
        }


        public void Refresh()
        {
            MakeUpdateFleet();
        }

        private void ConsumeBaseAirlineModel(AirlineModel airlineModel)
        {
            foreach (var field in typeof(AirlineModel).GetFields(HarmonyLib.AccessTools.all))
            {
                field.SetValue(this, field.GetValue(airlineModel));
            }
        }

        private static bool AircraftAvailable(string aircraftName)
        {
            for (int i = 0; i < Singleton<AirTrafficController>.Instance.aircraftModels.Length; i++)
            {
                if (Singleton<AirTrafficController>.Instance.aircraftModels[i].aircraftType.Equals(aircraftName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateDefaultFleets(List<(string, int)> fleetAndCounts)
        {
            ParentModel.aircraftFleetModels = new string[fleetAndCounts.Count];
            ParentModel.fleetCount = new int[fleetAndCounts.Count];
            for (int i = 0; i < fleetAndCounts.Count; i++)
            {
                (string model, int count) = fleetAndCounts[i];
                ParentModel.aircraftFleetModels[i] = model;
                ParentModel.fleetCount[i] = count;
            }
        }

        private void MakeUpdateFleet()
        {
            if (!_shouldGenerateFleet)
            {
                return; // We've already created
            }

            AirportCEOTweaksCore.LogDebug($"Starting {nameof(MakeUpdateFleet)} for airline \"{ParentModel.businessName}\"");

            try
            {
                List<(string, int)> aircraftTypesCounts = new();
                bool hasTweaksFleet = airlineBusinessData.tweaksFleet != null && airlineBusinessData.tweaksFleetCount != null && airlineBusinessData.tweaksFleet.Length == airlineBusinessData.tweaksFleetCount.Length;

                if (hasTweaksFleet)
                {
                    for (int i = 0; i < airlineBusinessData.tweaksFleet.Length; i++)
                    {   
                        if (!AircraftAvailable(airlineBusinessData.tweaksFleet[i]))
                        {
                            continue;
                        }

                        aircraftTypesCounts.Add((airlineBusinessData.tweaksFleet[i], airlineBusinessData.tweaksFleetCount[i]));
                    }

                    _shouldGenerateFleet = false;
                }
                else if (ParentModel.aircraftFleetModels.Length == ParentModel.fleetCount.Length)
                {
                    for (int i = 0; i < ParentModel.aircraftFleetModels.Length; i++)
                    {   
                        if (!AircraftAvailable(ParentModel.aircraftFleetModels[i]))
                        {
                            continue;
                        }

                        aircraftTypesCounts.Add((ParentModel.aircraftFleetModels[i], ParentModel.fleetCount[i]));
                    }
                    _shouldGenerateFleet = false;
                }
                else
                {
                    AirportCEOTweaksCore.LogError($"No valid source of aircraft fleet/fleet count for airline \"{ParentModel.businessName}\" - size mismatch exists! Creating temporary");
                    for (int i = 0; i < ParentModel.aircraftFleetModels.Length; i++)
                    {   
                        if (!AircraftAvailable(ParentModel.aircraftFleetModels[i]))
                        {
                            continue;
                        }

                        aircraftTypesCounts.Add((ParentModel.aircraftFleetModels[i], 5)); // using 5 as a generic value!
                    }
                }

                UpdateDefaultFleets(aircraftTypesCounts);

                AirlineFleetMembers = new();

                AirportCEOTweaksCore.LogInfo($"c1: {airlineBusinessData.tweaksFleet == null}. l1: {aircraftTypesCounts.Count}");

                foreach ((string model, int count) in aircraftTypesCounts)
                {
                    AirlineFleetMember member = new AirlineFleetMember(ParentModel, model, count);

                    if (member.ErrorFlag)
                    {
                        continue;
                    }

                    AirlineFleetMembers.Add(member);
                }
            }
            catch (Exception ex)
            {
                AirportCEOTweaksCore.LogError($"Failed to create tweaks fleet. {ExceptionUtils.ProccessException(ex)}");
            }
        }

        //public string GetAndAllocateRandomAircraft(bool allocate = true)
        //{
        //    string aircraft;
        //    SortedDictionary<float, string> lotto = new SortedDictionary<float, string>();
        //    float counter = 0;
        //    foreach (string key in AircraftTypeAllocation.Keys)
        //    {
        //        if (Singleton<ModsController>.Instance.CanServeAircraftType(key))
        //        {
        //            float numToAdd = AircraftTypeAllocation[key].Available + counter;
        //            lotto.Add(numToAdd, key);
        //            counter += AircraftTypeAllocation[key].Available;
        //        }
        //    }

        //    float pick = Random.Range(0, counter);

        //    foreach (float number in lotto.Keys)
        //    {
        //        if (number >= pick)
        //        {
        //            aircraft = lotto[number];
        //            if (allocate) { AircraftTypeAllocation[aircraft] = (AircraftTypeAllocation[aircraft].Available, AircraftTypeAllocation[aircraft].Allocated + 1); }
        //            return aircraft;
        //        }
        //    }
        //    return "ERROR NO AIRCRAFT AVAILABLE";
        //}
        //private void MakeUpdateFleetAllocations()
        //{
        //    for (int i = 0; i < aircraftFleetModels.Length ; i++)
        //    {
        //        string aircraft = aircraftFleetModels[i];
        //        float count = (i < fleetCount.Length) ? fleetCount[i] : 0f ;
        //        if (! AircraftTypeAllocation.ContainsKey(aircraft))
        //        {
        //            AircraftTypeAllocation.Add(aircraft, (0, 0));
        //        }
        //        AircraftTypeData aTD = AirportCEOTweaksCore.aircraftTypeDataDict[aircraft];
        //        AircraftTypeAllocation[aircraft] = (aTD.GetAllocationCount() * count, 0);
        //    }
        //    foreach (string aircraft in AircraftTypeAllocation.Keys)
        //    {
        //        if (! aircraftFleetModels.Contains(aircraft))
        //        {
        //            AircraftTypeAllocation.Remove(aircraft);
        //        }
        //    }
        //    foreach (CommercialFlightModel cFM in flightListObjects)
        //    {
        //        if (cFM.arrivalTimeDT - Singleton<TimeController>.Instance.GetCurrentContinuousTime() < new TimeSpan(24*AircraftTypeDataUtilities.AllocationBaselineDays, 0, 0)) 
        //        {
        //            AircraftTypeAllocation[cFM.aircraftTypeString] = (AircraftTypeAllocation[cFM.aircraftTypeString].Available, AircraftTypeAllocation[cFM.aircraftTypeString].Allocated + 1f);
        //        }
        //    }
        //}

        private Country[] CountryRetriever(string[] codes)
        {
            if (codes == null || codes.Length==0)
            {
                return null;
            }

            HashSet<string> codeList = new HashSet<string>(codes);

            List<Country> countryList = new();
            foreach (string code in codeList)
            {
                try
                {
                    Country country = TravelController.GetCountryByCode(code);
                    if (country != null && !countryList.Contains(country))
                    {
                        countryList.Add(country);
                    }
                }
                catch
                {
                    if (!string.IsNullOrEmpty(code))
                    {
                        Debug.LogError("ACEO Tweaks | ERROR: In airline " + ParentModel.businessName + " could not get country for counrty code!");
                    }
                }
            }

            return countryList.ToArray();
        }    
    }
}
