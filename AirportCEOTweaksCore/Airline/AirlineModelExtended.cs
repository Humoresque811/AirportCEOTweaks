using AirportCEOModLoader.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity;
using UnityEngine;

namespace AirportCEOTweaksCore
{
    public class AirlineModelExtended : AirlineModel
    {
        // This block of props are always filled in! They can always safely be used to get the correct information
        private AirlineBusinessData airlineBusinessData { get; set; }
        public AirlineModel OldParentModel { get; private set; }
        public Country[] HomeCountries { get; private set; }
        public List<AirlineFleetMember> AirlineFleetMembers { get; private set; }
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
        public bool IsCustom { get; private set; }

        private bool _fleetGenerated = false;

        //public Enums.BusinessClass starRank;
        //public float economyTier = 2;
        //public HashSet<PAXCommercialFlightModelExtended> myFlights;
        //public float cargoProportion = 0f;
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

            OldParentModel = airlineModel;

            Singleton<BusinessController>.Instance.RemoveFromBusinessList(this);
            //Singleton<BusinessController>.Instance.RemoveFromBusinessList(airlineModel);
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
            airline.fleet = aircraftFleetModels;
            if (airlineBusinessData.arrayHomeCountryCodes == null || airlineBusinessData.arrayHomeCountryCodes.Length == 0)
            {
                HomeCountries = CountryRetriever([airline.countryCode]);
            }
            else
            {
                HomeCountries = CountryRetriever(airlineBusinessData.arrayHomeCountryCodes);
            }

            IsCustom = airline.isCustom;
            //AirportCEOTweaksCore.LogDebug($"{businessName} is custom {IsCustom}");
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
            aircraftFleetModels = new string[fleetAndCounts.Count];
            fleetCount = new int[fleetAndCounts.Count];
            for (int i = 0; i < fleetAndCounts.Count; i++)
            {
                (string model, int count) = fleetAndCounts[i];
                aircraftFleetModels[i] = model;
                fleetCount[i] = count;
            }
        }

        private void MakeUpdateFleet()
        {
            if (_fleetGenerated)
            {
                return; // We've already created
            }

            //AirportCEOTweaksCore.LogDebug($"Starting {nameof(MakeUpdateFleet)} for airline \"{businessName}\"");

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

                    _fleetGenerated = true;
                }
                else if (OldParentModel.aircraftFleetModels.Length == OldParentModel.fleetCount.Length)
                {
                    for (int i = 0; i < OldParentModel.aircraftFleetModels.Length; i++)
                    {   
                        if (!AircraftAvailable(OldParentModel.aircraftFleetModels[i]))
                        {
                            continue;
                        }

                        aircraftTypesCounts.Add((OldParentModel.aircraftFleetModels[i], OldParentModel.fleetCount[i]));
                    }
                    _fleetGenerated = true;
                }
                else
                {
                    AirportCEOTweaksCore.LogError($"No valid source of aircraft fleet/fleet count for airline \"{businessName}\" - size mismatch exists! Creating temporary");
                    for (int i = 0; i < OldParentModel.aircraftFleetModels.Length; i++)
                    {   
                        if (!AircraftAvailable(OldParentModel.aircraftFleetModels[i]))
                        {
                            continue;
                        }

                        aircraftTypesCounts.Add((OldParentModel.aircraftFleetModels[i], 5)); // using 5 as a generic value!
                    }
                }

                UpdateDefaultFleets(aircraftTypesCounts);
                AirlineFleetMembers = new();

                foreach ((string model, int count) in aircraftTypesCounts)
                {
                    AirlineFleetMember member = new AirlineFleetMember(this, model, count);

                    if (member.ErrorFlag)
                    {
                        AirportCEOTweaksCore.LogError($"AirlineFleetMember \"{model}\" for airline \"{this.businessName}\" failed to generate.");
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
                        Debug.LogError("ACEO Tweaks | ERROR: In airline " + businessName + " could not get country for counrty code!");
                    }
                }
            }

            return countryList.ToArray();
        }    
    }
}
