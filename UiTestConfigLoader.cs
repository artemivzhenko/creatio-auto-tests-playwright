using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreatioAutoTestsPlaywright.Config
{
    /// <summary>
    /// Helper class for loading UI test configuration from JSON.
    /// </summary>
    public static class UiTestConfigLoader
    {
        /// <summary>
        /// Default JsonSerializer options used for reading configuration.
        /// </summary>
        private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

        /// <summary>
        /// Loads UI test configuration from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to JSON configuration file.</param>
        /// <returns>Deserialized UiTestConfig instance.</returns>
        public static UiTestConfig LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Configuration file path must be provided.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: '{filePath}'.", filePath);
            }

            string json;

            try
            {
                json = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to read configuration file '{filePath}'. See inner exception for details.",
                    ex);
            }

            try
            {
                var config = JsonSerializer.Deserialize<UiTestConfig>(json, DefaultOptions);
                if (config == null)
                {
                    throw new InvalidOperationException(
                        $"Configuration file '{filePath}' was deserialized to null.");
                }

                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse configuration JSON from file '{filePath}'. " +
                    $"Ensure the JSON structure matches UiTestConfig DTOs.",
                    ex);
            }
        }

        /// <summary>
        /// Loads UI test configuration from a JSON string.
        /// </summary>
        /// <param name="json">JSON content representing UiTestConfig.</param>
        /// <returns>Deserialized UiTestConfig instance.</returns>
        public static UiTestConfig LoadFromJson(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON content must not be empty.", nameof(json));
            }

            try
            {
                var config = JsonSerializer.Deserialize<UiTestConfig>(json, DefaultOptions);
                if (config == null)
                {
                    throw new InvalidOperationException(
                        "Configuration JSON was deserialized to null.");
                }

                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Failed to parse configuration JSON string. " +
                    "Ensure the JSON structure matches UiTestConfig DTOs.",
                    ex);
            }
        }

        /// <summary>
        /// Creates default JsonSerializer options for configuration loading.
        /// </summary>
        private static JsonSerializerOptions CreateDefaultOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Ensure attributes like [JsonPropertyName] are respected.
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}