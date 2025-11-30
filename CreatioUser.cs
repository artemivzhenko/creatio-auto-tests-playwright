using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Environment
{
    /// <summary>
    /// Represents a single Creatio user used in tests.
    /// Each user maintains its own HttpClient, CookieContainer and cookies.
    /// </summary>
    public sealed class CreatioUser : IDisposable
    {
        /// <summary>
        /// Creatio login (user name).
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Creatio password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Base URL of the Creatio instance (for example https://dev-example.creatio.com).
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Authentication URL (for example /ServiceModel/AuthService.svc/Login).
        /// </summary>
        public string AuthUrl { get; }

        /// <summary>
        /// Cookies received after login (name -> value).
        /// </summary>
        public IReadOnlyDictionary<string, string> Cookies => _cookies;

        /// <summary>
        /// HttpClient configured with BaseAddress and CookieContainer for this user.
        /// Can be used to send REST calls to Creatio on behalf of this user.
        /// </summary>
        public HttpClient HttpClient => _httpClient;

        private readonly CookieContainer _cookieContainer;
        private readonly Dictionary<string, string> _cookies;
        private readonly HttpClient _httpClient;

        public CreatioUser(
            string baseUrl,
            string authUrl,
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl must be provided.", nameof(baseUrl));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username must be provided.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password must be provided.", nameof(password));
            }

            BaseUrl = baseUrl.TrimEnd('/');
            AuthUrl = string.IsNullOrWhiteSpace(authUrl)
                ? BuildDefaultAuthUrl(BaseUrl)
                : authUrl;
            Username = username;
            Password = password;

            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                UseDefaultCredentials = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };

            _cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Perform login immediately so that Cookies and HttpClient are ready to use.
            RefreshSessionAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Forces a new login and refreshes cookies and HttpClient session state.
        /// </summary>
        public void RefreshSession()
        {
            RefreshSessionAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously forces a new login and refreshes cookies and HttpClient session state.
        /// </summary>
        public async Task RefreshSessionAsync()
        {
            var authUri = new Uri(AuthUrl, UriKind.RelativeOrAbsolute);
            if (!authUri.IsAbsoluteUri)
            {
                authUri = new Uri(new Uri(BaseUrl), authUri);
            }

            var authPayload = new
            {
                UserName = Username,
                UserPassword = Password
            };

            var json = JsonSerializer.Serialize(authPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(authUri, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            _cookies.Clear();
            var baseUri = new Uri(BaseUrl);
            var cookies = _cookieContainer.GetCookies(baseUri);
            foreach (Cookie cookie in cookies)
            {
                _cookies[cookie.Name] = cookie.Value;
            }

            if (_cookies.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No cookies were received after Creatio login for user '{Username}'.");
            }
        }

        private static string BuildDefaultAuthUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl must be provided to build default AuthUrl.", nameof(baseUrl));
            }

            return baseUrl.TrimEnd('/') + "/ServiceModel/AuthService.svc/Login";
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
