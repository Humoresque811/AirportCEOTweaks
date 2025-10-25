using System;
using System.IO;
using UnityEngine;

namespace AirportCEOTweaksCore.Util;

/// <summary>
/// Provides JSON validation utilities for aircraft livery files.
/// </summary>
public static class JsonValidator
{
    /// <summary>
    /// Validates all JSON files in immediate subdirectories of the specified path.
    /// Only validates the first JSON file found in each subdirectory for syntactic correctness.
    /// </summary>
    /// <param name="filePath">Path to directory containing livery subdirectories</param>
    /// <returns>True if all JSON files are valid, false if any are invalid or missing</returns>
    public static bool ValidateAllJsonInDirectory(string filePath)
    {
        Debug.Log($"[JSON Validator] Starting validation for path: {filePath}");

        // Normalize path separators
        filePath = filePath.Replace("\\", "/");

        // Verify directory exists
        if (!Directory.Exists(filePath))
        {
            Debug.LogWarning($"[JSON Validator] Directory does not exist: {filePath}");
            return false;
        }

        // Get immediate subdirectories only
        string[] directories = Directory.GetDirectories(filePath);

        if (directories.Length == 0)
        {
            Debug.LogWarning($"[JSON Validator] No subdirectories found in: {filePath}");
            return false;
        }

        // Validate first JSON file in each subdirectory
        int validCount = 0;
        int invalidCount = 0;
        int missingCount = 0;

        foreach (string dir in directories)
        {
            string[] jsonFiles = Directory.GetFiles(dir, "*.json");

            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"[JSON Validator] No JSON found in: {dir}");
                missingCount++;
                continue;
            }

            // Only validate the first JSON file
            string jsonFile = jsonFiles[0];
            if (ValidateJsonFile(jsonFile))
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }
        }

        bool allValid = invalidCount == 0 && missingCount == 0 && validCount > 0;
        Debug.Log($"[JSON Validator] Validation complete for {filePath}: {validCount} valid, {invalidCount} invalid, {missingCount} missing - Result: {(allValid ? "PASS" : "FAIL")}");
        return allValid;
    }

    /// <summary>
    /// Validates a single JSON file for syntactic correctness.
    /// </summary>
    /// <param name="jsonFile">Path to JSON file</param>
    /// <returns>True if valid, false if invalid</returns>
    private static bool ValidateJsonFile(string jsonFile)
    {
        try
        {
            string content = Utils.ReadFile(jsonFile);

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError($"[JSON Validator] Invalid JSON: {jsonFile} — File is empty");
                return false;
            }

            // Parse JSON using the same method as actual livery loading
            // This ensures validation matches runtime deserialization exactly
            LiveryData liveryData = Utils.CreateFromJSON<LiveryData>(content);

            // Ensure data was actually parsed
            if (liveryData == null)
            {
                Debug.LogError($"[JSON Validator] Invalid JSON: {jsonFile} — Deserialization returned null");
                return false;
            }

            Debug.Log($"[JSON Validator] Valid JSON: {jsonFile}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JSON Validator] Invalid JSON: {jsonFile} — {ex.Message}");
            return false;
        }
    }
}


