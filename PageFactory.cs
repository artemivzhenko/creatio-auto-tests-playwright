using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CreatioAutoTestsPlaywright.Config;
using CreatioAutoTestsPlaywright.Environment;
using Microsoft.Playwright;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Factory that creates a fully initialized PageContext from JSON configuration.
    /// It creates CreatioPage, opens the URL and builds all fields and buttons.
    /// </summary>
    public static class PageFactory
    {
        /// <summary>
        /// Asynchronously creates and initializes a PageContext for the given page configuration.
        /// A new CreatioPage will be created for the specified environment and browser.
        /// </summary>
        /// <param name="config">Page configuration loaded from JSON.</param>
        /// <param name="env">Creatio environment used to resolve base URL and users.</param>
        /// <param name="browser">Playwright browser instance to create a context from.</param>
        /// <param name="debug">When true, CreatioPage will emit debug logs during initialization.</param>
        /// <returns>Initialized PageContext instance.</returns>
        public static async Task<PageContext> CreateAsync(
            PageConfig config,
            CreatioEnvironment env,
            IBrowser browser,
            bool debug = false)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            if (browser == null)
            {
                throw new ArgumentNullException(nameof(browser));
            }

            if (string.IsNullOrWhiteSpace(config.Url))
            {
                throw new ArgumentException("PageConfig.Url must not be empty.", nameof(config));
            }

            // Username is optional here. When null, CreatioPage.ResolveUser will pick a default user from environment.
            var page = new CreatioPage(
                path: config.Url,
                env: env,
                browser: browser,
                username: null);

            await page.InitializeAsync(debug).ConfigureAwait(false);

            var fields = new Dictionary<string, IField>(StringComparer.Ordinal);
            if (config.Fields != null)
            {
                foreach (var fieldCfg in config.Fields)
                {
                    var field = FieldFactory.CreateField(page, fieldCfg);
                    fields[field.Code] = field;
                }
            }

            var buttons = new Dictionary<string, IButton>(StringComparer.Ordinal);
            if (config.Buttons != null)
            {
                foreach (var buttonCfg in config.Buttons)
                {
                    var button = ButtonFactory.CreateButton(page, buttonCfg);
                    buttons[button.Code] = button;
                }
            }

            return new PageContext(page, config, fields, buttons);
        }

        /// <summary>
        /// Synchronous wrapper over CreateAsync for cases when async is not convenient in tests.
        /// </summary>
        /// <param name="config">Page configuration loaded from JSON.</param>
        /// <param name="env">Creatio environment used to resolve base URL and users.</param>
        /// <param name="browser">Playwright browser instance to create a context from.</param>
        /// <param name="debug">When true, CreatioPage will emit debug logs during initialization.</param>
        /// <returns>Initialized PageContext instance.</returns>
        public static PageContext Create(
            PageConfig config,
            CreatioEnvironment env,
            IBrowser browser,
            bool debug = false)
        {
            return CreateAsync(config, env, browser, debug).GetAwaiter().GetResult();
        }
    }
}