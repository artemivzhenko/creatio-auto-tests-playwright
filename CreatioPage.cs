using CreatioAutoTestsPlaywright.Environment;
using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Represents a live Creatio page opened in Playwright.
    /// Responsible for creating a browser context for a specific user,
    /// applying cookies from CreatioEnvironment and navigating to the desired URL.
    /// Also waits for Creatio loading animation to disappear.
    /// </summary>
    public sealed class CreatioPage : IAsyncDisposable
    {
        public string Path { get; }

        public string FullUrl { get; }

        public CreatioEnvironment Env { get; }

        public CreatioUser User { get; }

        public IBrowser Browser { get; }

        public IBrowserContext Context { get; private set; } = null!;

        public IPage Page { get; private set; } = null!;

        public CreatioPage(
            string path,
            CreatioEnvironment env,
            IBrowser browser,
            string? username = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must be provided.", nameof(path));
            }

            Env = env ?? throw new ArgumentNullException(nameof(env));
            Browser = browser ?? throw new ArgumentNullException(nameof(browser));

            var normalizedPath = NormalizeRelativePath(path);
            Path = normalizedPath;
            FullUrl = CombineBaseUrlAndPath(Env.BaseUrl, normalizedPath);

            User = ResolveUser(env, username);
        }

        /// <summary>
        /// Creates a browser context for the user, applies cookies and opens the page.
        /// Also waits for Creatio loading overlay (#loading-animation) to disappear.
        /// </summary>
        public async Task InitializeAsync(bool debug = false)
        {
            if (Context != null && Page != null)
            {
                return;
            }

            var baseUri = new Uri(Env.BaseUrl);

            var contextOptions = new BrowserNewContextOptions
            {
                BaseURL = Env.BaseUrl
            };

            Context = await Browser.NewContextAsync(contextOptions).ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write($"[CreatioPage] BaseUrl: {Env.BaseUrl}");
                FieldLogger.Write($"[CreatioPage] Path: {Path}");
                FieldLogger.Write($"[CreatioPage] User: {User.Username}");
                FieldLogger.Write($"[CreatioPage] Cookies count in CreatioUser: {User.Cookies.Count}");
            }

            if (User.Cookies.Count == 0 && debug)
            {
                FieldLogger.Write("[CreatioPage] WARNING: user has no cookies, navigation will be anonymous.");
            }

            if (User.Cookies.Count > 0)
            {
                var cookies = new List<Cookie>();

                foreach (var kvp in User.Cookies)
                {
                    cookies.Add(new Cookie
                    {
                        Name = kvp.Key,
                        Value = kvp.Value,
                        Domain = baseUri.Host,
                        Path = "/"
                    });
                }

                await Context.AddCookiesAsync(cookies).ConfigureAwait(false);

                if (debug)
                {
                    var ctxCookies = await Context.CookiesAsync().ConfigureAwait(false);
                    FieldLogger.Write("[CreatioPage] Cookies in Playwright context:");
                    foreach (var c in ctxCookies)
                    {
                        FieldLogger.Write($"  - {c.Name}={c.Value}; domain={c.Domain}; path={c.Path}");
                    }
                }
            }

            Page = await Context.NewPageAsync().ConfigureAwait(false);

            var response = await Page.GotoAsync(Path, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            }).ConfigureAwait(false);

            if (debug)
            {
                if (response == null)
                {
                    FieldLogger.Write("[CreatioPage] GotoAsync returned null response.");
                }
                else
                {
                    FieldLogger.Write($"[CreatioPage] GotoAsync: Status={response.Status}, Url={response.Url}");
                }
            }

            await WaitForPageLoadedAsync(debug).ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write($"[CreatioPage] Page.Url after navigation: {Page.Url}");

                var content = await Page.ContentAsync().ConfigureAwait(false);
                var snippetLength = Math.Min(content.Length, 500);
                var snippet = content.Substring(0, snippetLength);

                FieldLogger.Write("[CreatioPage] Page content (first 500 chars):");
                FieldLogger.Write(snippet);
            }
        }

        public void Initialize(bool debug = false)
        {
            InitializeAsync(debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Reload current page and wait for Creatio loading overlay to disappear.
        /// </summary>
        public async Task ReloadAsync(bool debug = false)
        {
            if (Page == null)
            {
                throw new InvalidOperationException("Page is not initialized. Call InitializeAsync() first.");
            }

            var response = await Page.ReloadAsync(new PageReloadOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            }).ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write($"[CreatioPage] ReloadAsync: Status={response?.Status}, Url={Page.Url}");
            }

            await WaitForPageLoadedAsync(debug).ConfigureAwait(false);
        }

        public void Reload(bool debug = false)
        {
            ReloadAsync(debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Save current page HTML to file for debugging.
        /// </summary>
        public async Task SaveHtmlAsync(string filePath)
        {
            if (Page == null)
            {
                throw new InvalidOperationException("Page is not initialized.");
            }

            var content = await Page.ContentAsync().ConfigureAwait(false);
            System.IO.File.WriteAllText(filePath, content);
        }

        /// <summary>
        /// Wait until Creatio loading overlay (#loading-animation) disappears.
        /// If element never exists, wait will finish immediately.
        /// </summary>
        private async Task WaitForPageLoadedAsync(bool debug)
        {
            if (Page == null)
            {
                throw new InvalidOperationException("Page is not initialized.");
            }

            try
            {
                var loading = Page.Locator("#loading-animation");
                await loading.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Detached,
                    Timeout = 60_000
                }).ConfigureAwait(false);

                if (debug)
                {
                    FieldLogger.Write("[CreatioPage] WaitForPageLoadedAsync: #loading-animation detached.");
                }
            }
            catch (TimeoutException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[CreatioPage] WaitForPageLoadedAsync timeout: {ex.Message}");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[CreatioPage] WaitForPageLoadedAsync Playwright error: {ex.Message}");
                }
            }
        }

        private static CreatioUser ResolveUser(CreatioEnvironment env, string? username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                return env.GetUser(username);
            }

            if (env.Users.Count == 0)
            {
                throw new InvalidOperationException("CreatioEnvironment has no registered users.");
            }

            var user = env.Users.Values.FirstOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("Could not resolve default user from CreatioEnvironment.");
            }

            return user;
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

        public async ValueTask DisposeAsync()
        {
            if (Page != null)
            {
                await Page.CloseAsync().ConfigureAwait(false);
            }

            if (Context != null)
            {
                await Context.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
