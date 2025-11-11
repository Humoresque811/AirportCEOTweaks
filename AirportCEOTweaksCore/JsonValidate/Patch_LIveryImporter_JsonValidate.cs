

using AirportCEOTweaksCore.Util;
using HarmonyLib;
using UnityEngine;

namespace AirportCEOTweaksCore.JsonValidate;

/// <summary>
/// Patches LiveryImporter.LoadCustomLivery to only validate JSON syntax
/// without loading any assets, GameObjects, textures, or sprites.
/// </summary>
[HarmonyPatch(typeof(LiveryImporter))]
internal class Patch_LiveryImporter_JsonValidate
{
    /// <summary>
    /// Prefix patch that validates JSON before LoadCustomLivery runs.
    /// </summary>
    /// <param name="filePath">Path to directory containing livery subdirectories</param>
    /// <param name="airlineName">Name of airline (not used in validation)</param>
    /// <returns>True if JSON is valid (allow original method), false if invalid (skip original method)</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(LiveryImporter.LoadCustomLivery))]
    public static bool Prefix(string filePath, string airlineName)
    {
        Debug.Log($"[JSON Validator] LoadCustomLivery called with airline: {airlineName}");

        // Validate JSON - returns true if all valid
        bool isValid = JsonValidator.ValidateAllJsonInDirectory(filePath);

        // Return the validation result:
        // true = allow original method to run (JSON is valid)
        // false = skip original method (JSON has errors)
        return isValid;
    }
}

