using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CreatioAutoTestsPlaywright.Environment
{
    /// <summary>
    /// Represents a Creatio environment (single Creatio instance) with multiple users.
    /// Responsible for holding base URLs and a map of users that can be used in tests.
    /// All users are created (and logged in) during environment construction.
    /// </summary>
    public sealed class CreatioEnvironment : IDisposable
    {
        /// <summary>
        /// Base URL of the Creatio instance, for example https://dev-example.creatio.com.
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Authentication URL used for all users.
        /// If not specified in configuration, defaults to {BaseUrl}/ServiceModel/AuthService.svc/Login.
        /// </summary>
        public string AuthUrl { get; }

        /// <summary>
        /// Registered users for this environment.
        /// Key is the user's Creatio login (Username), value is CreatioUser instance.
        /// </summary>
        public IReadOnlyDictionary<string, CreatioUser> Users => _users;

        private readonly Dictionary<string, CreatioUser> _users;

        #region Constructors

        /// <summary>
        /// Creates a Creatio environment with a single user.
        /// AuthUrl will be built as {BaseUrl}/ServiceModel/AuthService.svc/Login.
        /// </summary>
        public CreatioEnvironment(
            string baseUrl,
            string username,
            string password)
            : this(
                baseUrl: baseUrl,
                authUrl: BuildDefaultAuthUrl(baseUrl),
                users: new[]
                {
                    new CreatioUserConfig(username, password)
                })
        {
        }

        /// <summary>
        /// Creates a Creatio environment with a single user and explicit AuthUrl.
        /// </summary>
        public CreatioEnvironment(
            string baseUrl,
            string authUrl,
            string username,
            string password)
            : this(
                baseUrl: baseUrl,
                authUrl: authUrl,
                users: new[]
                {
                    new CreatioUserConfig(username, password)
                })
        {
        }

        /// <summary>
        /// Creates a Creatio environment with multiple users.
        /// Each user is logged in during construction and stored in the Users dictionary.
        /// </summary>
        public CreatioEnvironment(
            string baseUrl,
            string authUrl,
            IEnumerable<CreatioUserConfig> users)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl must be provided.", nameof(baseUrl));
            }

            BaseUrl = baseUrl.TrimEnd('/');
            AuthUrl = string.IsNullOrWhiteSpace(authUrl)
                ? BuildDefaultAuthUrl(BaseUrl)
                : authUrl;

            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            _users = new Dictionary<string, CreatioUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var cfg in users)
            {
                if (cfg == null)
                {
                    continue;
                }

                var user = new CreatioUser(BaseUrl, AuthUrl, cfg.Username, cfg.Password);
                _users[user.Username] = user;
            }

            if (_users.Count == 0)
            {
                throw new InvalidOperationException("CreatioEnvironment must have at least one user.");
            }
        }

        /// <summary>
        /// Creates a Creatio environment from a JObject configuration.
        /// Expected structure:
        /// {
        ///   "BaseUrl": "https://dev-example.creatio.com",
        ///   "AuthUrl": "https://dev-example.creatio.com/ServiceModel/AuthService.svc/Login", // optional
        ///   "Users": [
        ///     { "Username": "Supervisor", "Password": "Supervisor" },
        ///     { "Username": "User1", "Password": "User1" }
        ///   ]
        /// }
        /// Keys must match property names.
        /// </summary>
        public CreatioEnvironment(JObject config)
            : this(
                baseUrl: GetRequiredString(config, "BaseUrl"),
                authUrl: GetOptionalString(config, "AuthUrl") ?? BuildDefaultAuthUrl(GetRequiredString(config, "BaseUrl")),
                users: ParseUsers(config))
        {
        }

        /// <summary>
        /// Creates a Creatio environment from a JSON configuration file.
        /// The JSON structure must match the JObject constructor expectations.
        /// </summary>
        /// <param name="configJsonPath">Path to the JSON configuration file.</param>
        public CreatioEnvironment(string configJsonPath)
            : this(LoadConfigFromFile(configJsonPath))
        {
        }

        #endregion

        #region Public helpers

        /// <summary>
        /// Returns a CreatioUser by username or throws if the user does not exist.
        /// </summary>
        public CreatioUser GetUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username must be provided.", nameof(username));
            }

            if (!_users.TryGetValue(username, out var user))
            {
                throw new KeyNotFoundException(
                    $"User '{username}' is not registered in this CreatioEnvironment.");
            }

            return user;
        }

        /// <summary>
        /// Forces recreation of the session (login) for the specified user.
        /// </summary>
        public void RefreshUserSession(string username)
        {
            var user = GetUser(username);
            user.RefreshSession();
        }

        /// <summary>
        /// Asynchronously forces recreation of the session (login) for the specified user.
        /// </summary>
        public Task RefreshUserSessionAsync(string username)
        {
            var user = GetUser(username);
            return user.RefreshSessionAsync();
        }

        #endregion

        #region Internal helpers

        private static string BuildDefaultAuthUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl must be provided to build default AuthUrl.", nameof(baseUrl));
            }

            return baseUrl.TrimEnd('/') + "/ServiceModel/AuthService.svc/Login";
        }

        private static JObject LoadConfigFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Config path must be provided.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found at path '{path}'.", path);
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
                    $"Configuration is missing required property '{propertyName}'.",
                    nameof(obj));
            }

            var value = token.Type == JTokenType.String ? (string?)token : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Configuration property '{propertyName}' must be a non-empty string.",
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

        private static IEnumerable<CreatioUserConfig> ParseUsers(JObject obj)
        {
            var usersToken = obj["Users"];
            if (usersToken == null || usersToken.Type != JTokenType.Array)
            {
                throw new ArgumentException(
                    "Configuration must contain 'Users' array with user objects.",
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
                    "Configuration 'Users' array must contain at least one user.",
                    nameof(obj));
            }

            return list;
        }

        #endregion

        public void Dispose()
        {
            foreach (var user in _users.Values)
            {
                user.Dispose();
            }
        }
    }
}
