using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AirportCEOTweaksCore.Util;
using UnityEngine;

namespace AirportCEOTweaksCore.JsonValidate;

public static class ManualJsonValidating
{
    static bool isValidated = false;

    public static void ValidateJson()
    {
        // Check if the user has toggled the JSON validation config
        if (!isValidated && AirportCEOTweaksCoreConfig.ValidateJsonManual.Value)
        {
            isValidated = true;
            AirportCEOTweaksCore.LogInfo("Manual JSON validation triggered via config");

            var localDirectories = DirectoryHelpers.GetLocalModDirectories();
            var workshopDirectories = DirectoryHelpers.GetWorkshopDirectories();

            var directories = new List<string>();
            directories.AddRange(localDirectories);
            directories.AddRange(workshopDirectories);
            directories.Distinct();

            // Validate all registered aircraft paths
            foreach (string path in directories)
            {
                JsonValidator.ValidateAllJsonInDirectory(path);
            }

            AirportCEOTweaksCore.Instance.StartCoroutine(ResetToggleAfterDelay());

            AirportCEOTweaksCore.LogInfo("Manual JSON validation complete");
        }
    }


    private static IEnumerator ResetToggleAfterDelay()
    {
        // Wait for 1 seconds to avoid spamming the toggle
        yield return new WaitForSeconds(1f);
        // Reset the toggle to false
        AirportCEOTweaksCoreConfig.ValidateJsonManual.Value = false;
        isValidated = false;
    }

}