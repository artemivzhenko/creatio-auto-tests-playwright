using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CreatioAutoTestsPlaywright.Environment
{
    /// <summary>
    /// Represents site-wide Creatio configuration that is common for all environments.
    /// Contains relative paths for services and endpoints, such as auth, OData and process start.
    /// </summary>
    public sealed class CreatioSiteConfig
    {
        /// <summary>
        /// Relative path to the authentication service.
        /// For example: "/ServiceModel/AuthService.svc/Login".
        /// </summary>
        public string AuthPath { get; }

        /// <summary>
        /// Relative base path for OData services.
        /// For example: "/0/odata".
        /// </summary>
        public string? ODataBasePath { get; }

        /// <summary>
        /// Relative path to the business process start endpoint.
        /// For example: "/ServiceModel/ProcessEngineService.svc/ProcessEngineService/StartProcess".
        /// </summary>
        public string? ProcessEngine { get; }

        public CreatioSiteConfig(string authPath, string? odataBasePath, string? processEngine)
        {
            if (string.IsNullOrWhiteSpace(authPath))
            {
                throw new ArgumentException("AuthPath must be provided.", nameof(authPath));
            }

            AuthPath = NormalizeRelativePath(authPath);
            ODataBasePath = string.IsNullOrWhiteSpace(odataBasePath)
                ? null
                : NormalizeRelativePath(odataBasePath);
            processEngine = string.IsNullOrWhiteSpace(processEngine)
                ? null
                : NormalizeRelativePath(processEngine);
        }

        /// <summary>
        /// Creates configuration from a JObject.
        /// Expected JSON structure:
        /// {
        ///   "AuthPath": "/ServiceModel/AuthService.svc/Login",
        ///   "ODataBasePath": "/0/odata",
        ///   "processEngine": "/ServiceModel/ProcessEngineService.svc/ProcessEngineService/StartProcess"
        /// }
        /// Only AuthPath is required, others are optional.
        /// </summary>
        public CreatioSiteConfig(JObject config)
            : this(
                authPath: GetRequiredString(config, "AuthPath"),
                odataBasePath: GetOptionalString(config, "ODataBasePath"),
                processEngine: GetOptionalString(config, "processEngine"))
        {
        }

        /// <summary>
        /// Creates configuration from a JSON file on disk.
        /// </summary>
        /// <param name="configJsonPath">Path to the JSON configuration file.</param>
        public CreatioSiteConfig(string configJsonPath)
            : this(LoadConfigFromFile(configJsonPath))
        {
        }

        private static JObject LoadConfigFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Config path must be provided.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Site config file not found at path '{path}'.", path);
            }

            var json = File.ReadAllText(path);
            return JObject.Parse(json);
        }

        private static string GetRequiredString(JObject obj, string propertyName)
        {
            var token = obj[propertyName];
            if (token == null)
            {
                throw new ArgumentException(
                    $"Site configuration is missing required property '{propertyName}'.",
                    nameof(obj));
            }

            var value = token.Type == JTokenType.String ? (string?)token : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Site configuration property '{propertyName}' must be a non-empty string.",
                    nameof(obj));
            }

            return value;
        }

        private static string? GetOptionalString(JObject obj, string propertyName)
        {
            var token = obj[propertyName];
            if (token == null)
            {
                return null;
            }

            var value = token.Type == JTokenType.String ? (string?)token : token.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string NormalizeRelativePath(string path)
        {
            var trimmed = path.Trim();
            if (!trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                trimmed = "/" + trimmed;
            }

            return trimmed;
        }
    }
}
