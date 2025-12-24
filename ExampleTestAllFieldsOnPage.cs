using CreatioAutoTestsPlaywright.Config;
using CreatioAutoTestsPlaywright.Environment;
using CreatioAutoTestsPlaywright.Frontend;
using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.IO;

namespace CreatioAutoTestsPlaywright
{
    /// <summary>
    /// Sample test class that uses BaseCreatioTest.
    /// It provides JSON paths and then uses Env and users in tests.
    /// </summary>
    [TestFixture]
    public sealed class ExampleTestAllFieldsOnPage : BaseCreatioTest
    {
        protected override string WorkingDirectoryPath =>
            "C:\\Users\\aivzhenko\\source\\repos\\CreatioAutoTestsPlaywright\\";
        protected override string CreatioEnvConfigJson =>
            "dev.creatio.env.config.json";

        /// <summary>
        /// Runtime context for TestPage loaded from pages.config.json.
        /// Contains CreatioPage instance, field and button objects.
        /// </summary>
        public PageContext PageContext = null!;

        /// <summary>
        /// Shortcut to access underlying CreatioPage from PageContext.
        /// All existing tests that use Page will continue to work.
        /// </summary>
        public CreatioPage Page => PageContext.Page;

        [OneTimeSetUp]
        public async Task OneTimeSetUpPage()
        {
            // Load UI test configuration from JSON file.
            var configPath = Path.Combine(WorkingDirectoryPath, "C:\\Users\\aivzhenko\\source\\repos\\CreatioAutoTestsPlaywright\\pages.config.json");
            var uiConfig = UiTestConfigLoader.LoadFromFile(configPath);

            // Find TestPage by logical name.
            var pageConfig = uiConfig.Pages
                .FirstOrDefault(p => string.Equals(p.Name, "TestPage", StringComparison.Ordinal));

            if (pageConfig == null)
            {
                throw new InvalidOperationException(
                    "Page 'TestPage' was not found in pages.config.json.");
            }

            // Create and initialize PageContext (CreatioPage + fields + buttons) using PageFactory.
            PageContext = await PageFactory.CreateAsync(
                config: pageConfig,
                env: Env,
                browser: Browser,
                debug: true);
        }

        [OneTimeTearDown]
        public async Task TearDownPage()
        {
            if (PageContext != null && PageContext.Page != null)
            {
                await PageContext.Page.DisposeAsync();
            }
        }

        [Test]
        public void Environment_And_DefaultUser_Are_Initialized()
        {
            Assert.That(Env, Is.Not.Null, "Env is not initialized in SetUp.");
            Assert.That(SiteConfig, Is.Not.Null, "SiteConfig is not initialized in SetUp.");
            Assert.That(Env.Users.Count, Is.GreaterThan(0), "No users registered in environment.");
            Assert.That(DefaultUser, Is.Not.Null, "DefaultUser is not set.");

            // Example: get a specific user by username
            var supervisor = Env.GetUser("aivzhenko@deloittece.com");
            Assert.That(supervisor.Cookies.Count, Is.GreaterThan(0),
                "test has no cookies after environment initialization.");
        }

        /// <summary>
        /// Universal smoke-test that validates all fields from pages.config.json
        /// exist on the page and can be found by their locators.
        /// </summary>
        [Test]
        public async Task TestAllFieldsExist_FromConfig()
        {
            Assert.That(PageContext, Is.Not.Null, "PageContext is not initialized.");
            Assert.That(PageContext.Fields.Count, Is.GreaterThan(0),
                "No fields were loaded from configuration for page 'TestPage'.");

            foreach (var field in PageContext.Fields.Values)
            {
                var exists = await field.CheckIfExistAsync(debug: true);
                Assert.That(
                    exists,
                    Is.True,
                    $"Field '{field.Title}' (Code='{field.Code}') should exist on page '{PageContext.Config.Name}'.");
            }
        }
    }
}
