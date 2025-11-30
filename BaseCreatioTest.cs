using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using CreatioAutoTestsPlaywright.Environment;
using Microsoft.Playwright;

namespace CreatioAutoTestsPlaywright
{
    /// <summary>
    /// Base test class for Creatio business-flow tests.
    /// Reads environment and site configuration from JSON files,
    /// creates CreatioEnvironment and Playwright browser, and exposes them
    /// to derived test classes.
    /// </summary>
    public abstract class BaseCreatioTest
    {
        protected virtual string WorkingDirectoryPath =>
            ".\\";

        /// <summary>
        /// Path to JSON file with environment configuration (base URL and users).
        /// Derived classes can override this if they need a different configuration file.
        /// </summary>
        protected virtual string CreatioEnvConfigJson =>
            "creatio.env.config.json";

        /// <summary>
        /// Path to JSON file with site-wide configuration (relative service paths).
        /// Derived classes can override this if they need a different configuration file.
        /// </summary>
        protected virtual string CreatioSiteConfigJson =>
            "creatio.site.config.json";

        /// <summary>
        /// Launch options for Playwright browser.
        /// Derived classes can override this to customize browser configuration.
        /// </summary>
        protected virtual BrowserTypeLaunchOptions BrowserLaunchOptions =>
            new BrowserTypeLaunchOptions
            {
                Headless = true
            };

        /// <summary>
        /// Fully initialized Creatio environment for the current test run.
        /// Contains base URL and logged-in users.
        /// </summary>
        protected CreatioEnvironment Env = null!;

        /// <summary>
        /// Site-wide Creatio configuration with common service paths.
        /// </summary>
        protected CreatioSiteConfig SiteConfig = null!;

        /// <summary>
        /// Optional shortcut to the first registered user (for simple scenarios).
        /// Derived classes can ignore this and use Env.GetUser("Username") instead.
        /// </summary>
        protected CreatioUser DefaultUser = null!;

        /// <summary>
        /// Playwright root object.
        /// </summary>
        protected IPlaywright Playwright = null!;

        /// <summary>
        /// Shared Playwright browser instance for all tests in this fixture.
        /// </summary>
        protected IBrowser Browser = null!;

        /// <summary>
        /// One-time initialization for the whole test fixture:
        /// - loads JSON configuration,
        /// - creates CreatioEnvironment,
        /// - initializes Playwright and launches browser.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            SiteConfig = new CreatioSiteConfig(WorkingDirectoryPath + CreatioSiteConfigJson);

            var envConfig = LoadEnvConfig(WorkingDirectoryPath + CreatioEnvConfigJson);
            var baseUrl = GetRequiredString(envConfig, "BaseUrl");
            var userConfigs = ParseUsers(envConfig);

            var normalizedBaseUrl = baseUrl.TrimEnd('/');
            var authUrl = CombineBaseUrlAndPath(normalizedBaseUrl, SiteConfig.AuthPath);

            Env = new CreatioEnvironment(
                baseUrl: normalizedBaseUrl,
                authUrl: authUrl,
                users: userConfigs);

            using (var usersEnumerator = Env.Users.Values.GetEnumerator())
            {
                if (usersEnumerator.MoveNext())
                {
                    DefaultUser = usersEnumerator.Current;
                }
            }

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
            Browser = await Playwright.Chromium.LaunchAsync(BrowserLaunchOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// One-time cleanup for the whole test fixture:
        /// - closes Playwright browser,
        /// - disposes CreatioEnvironment,
        /// - disposes Playwright root.
        /// </summary>
        [OneTimeTearDown]
        public virtual async Task TearDown()
        {
            if (Browser != null)
            {
                await Browser.CloseAsync().ConfigureAwait(false);
            }

            Playwright?.Dispose();
            Env?.Dispose();
        }

        private static JObject LoadEnvConfig(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Environment config path must be provided.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Environment config file not found at path '{path}'.",
                    path);
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
                    $"Environment configuration is missing required property '{propertyName}'.",
                    nameof(obj));
            }

            var value = token.Type == JTokenType.String ? (string?)token : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Environment configuration property '{propertyName}' must be a non-empty string.",
                    nameof(obj));
            }

            return value;
        }

        private static IEnumerable<CreatioUserConfig> ParseUsers(JObject obj)
        {
            var usersToken = obj["Users"];
            if (usersToken == null || usersToken.Type != JTokenType.Array)
            {
                throw new ArgumentException(
                    "Environment configuration must contain 'Users' array with user objects.",
                    nameof(obj));
            }

            var list = new List<CreatioUserConfig>();

            foreach (var item in usersToken)
            {
                if (item is not JObject userObj)
                {
                    continue;
                }

                var username = GetRequiredString(userObj, "Username");
                var password = GetRequiredString(userObj, "Password");
                list.Add(new CreatioUserConfig(username, password));
            }

            if (list.Count == 0)
            {
                throw new ArgumentException(
                    "Environment configuration 'Users' array must contain at least one user.",
                    nameof(obj));
            }

            return list;
        }

        private static string CombineBaseUrlAndPath(string baseUrl, string relativePath)
        {
            var trimmedBase = baseUrl.TrimEnd('/');
            var trimmedPath = relativePath.Trim();

            if (!trimmedPath.StartsWith("/", StringComparison.Ordinal))
            {
                trimmedPath = "/" + trimmedPath;
            }

            return trimmedBase + trimmedPath;
        }
    }
}
