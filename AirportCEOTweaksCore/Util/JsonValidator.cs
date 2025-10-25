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
        Log($"Starting validation for path: {DirectoryHelpers.SafeDirectoryLog(filePath)}");

        // Normalize path separators
        filePath = filePath.Replace("\\", "/");

        // Verify directory exists
        if (!Directory.Exists(filePath))
        {
            LogWarning($"Directory does not exist: {DirectoryHelpers.SafeDirectoryLog(filePath)}");
            return false;
        }

        // Get immediate subdirectories only
        string[] directories = Directory.GetDirectories(filePath);

        if (directories.Length == 0)
        {
            LogWarning($"No subdirectories found in: {DirectoryHelpers.SafeDirectoryLog(filePath)}");
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
                LogWarning($"No JSON found in: {DirectoryHelpers.SafeDirectoryLog(dir)}");
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
        Log($"Validation complete for {DirectoryHelpers.SafeDirectoryLog(filePath)}: {validCount} valid, {invalidCount} invalid, {missingCount} missing - Result: {(allValid ? "PASS" : "FAIL")}");
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
                LogError($"Invalid JSON: {DirectoryHelpers.SafeDirectoryLog(jsonFile)} — File is empty");
                return false;
            }

            // Parse JSON using the same method as actual livery loading
            // This ensures validation matches runtime deserialization exactly
            LiveryData liveryData = Utils.CreateFromJSON<LiveryData>(content);

            // Ensure data was actually parsed
            if (liveryData == null)
            {
                LogError($"Invalid JSON: {DirectoryHelpers.SafeDirectoryLog(jsonFile)} — Deserialization returned null");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Invalid JSON: {DirectoryHelpers.SafeDirectoryLog(jsonFile)} — {ex.Message}");
            return false;
        }
    }

    static void Log(string message) => AirportCEOTweaksCore.Log($"[JSON Validator] {message}");
    static void LogError(string message) => AirportCEOTweaksCore.LogError($"[JSON Validator] {message}");
    static void LogWarning(string message) => AirportCEOTweaksCore.LogWarning($"[JSON Validator] {message}");
}


