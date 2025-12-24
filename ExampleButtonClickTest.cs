using System;
using System.Threading.Tasks;
using CreatioAutoTestsPlaywright.Environment;
using CreatioAutoTestsPlaywright.Frontend;
using Microsoft.Playwright;
using NUnit.Framework;

namespace CreatioAutoTestsPlaywright
{
    /// <summary>
    /// Tests that verify behaviour of Save button on a Creatio Freedom UI card.
    /// These tests do NOT use pages.config.json and rely on hard-coded field and button codes.
    /// </summary>
    [TestFixture]
    public sealed class ExampleButtonClickTest : BaseCreatioTest
    {
        protected override string WorkingDirectoryPath =>
            "C:\\Users\\aivzhenko\\source\\repos\\CreatioAutoTestsPlaywright\\";
        protected override string CreatioEnvConfigJson =>
            "dev.creatio.env.config.json";

        /// <summary>
        /// Underlying Creatio Freedom UI page used in button behaviour tests.
        /// </summary>
        public CreatioPage Page = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUpPage()
        {
            // Use the same test page as in other examples.
            Page = new CreatioPage(
                path: "/0/Shell/#Card/UsrTestPage_FormPage/edit/fe8432a7-4917-444d-a934-600ac815ac3b",
                env: Env,
                browser: Browser,
                username: "aivzhenko@deloittece.com");

            await Page.InitializeAsync(debug: true);
        }

        [OneTimeTearDown]
        public async Task TearDownPage()
        {
            if (Page != null)
            {
                await Page.DisposeAsync();
            }
        }

        /// <summary>
        /// Fills required text field, clicks the Save button and verifies
        /// that the Save button disappears from the page after successful save.
        /// </summary>
        [Test]
        public async Task SaveButton_HidesAfterSuccessfulSave()
        {
            // Arrange: required Name text field.
            var nameField = new TextField(
                page: Page,
                title: "Name",
                code: "Input_zsrmb1i",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Text);

            var fieldOk = await nameField.CheckFieldAsync(debug: true);
            Assert.That(fieldOk, Is.True, "Name text field should exist and match basic properties before save.");

            var valueToSet = $"AutoTest_Save_{Guid.NewGuid():N}";
            await nameField.SetValueAsync(valueToSet, debug: true);

            var currentValue = await nameField.GetValueAsync(debug: true);
            Assert.That(currentValue, Is.EqualTo(valueToSet), "Name field should contain the value before clicking Save.");

            // Arrange: Save button. Code = element-name, Title = visible caption.
            var saveButton = new Button(
                page: Page,
                title: "Save",
                code: "SaveButton");

            var existsBefore = await saveButton.CheckIfExistAsync(debug: true);
            Assert.That(existsBefore, Is.True, "Save button should be visible before clicking.");

            // Act: click Save.
            await saveButton.ClickAsync(debug: true);

            // Wait at low level until SaveButton is detached from DOM.
            var playwrightPage = Page.Page;
            try
            {
                await playwrightPage.WaitForSelectorAsync(
                    "crt-button[element-name='SaveButton']",
                    new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Detached,
                        Timeout = 15000
                    });
            }
            catch (TimeoutException)
            {
                Assert.Fail("Save button did not disappear within timeout after clicking.");
            }
            catch (PlaywrightException ex)
            {
                Assert.Fail($"Playwright error while waiting for Save button to disappear: {ex.Message}");
            }

            // Additionally, confirm via high-level Button API that it is no longer present.
            var existsAfter = await saveButton.CheckIfExistAsync(debug: true);
            Assert.That(existsAfter, Is.False, "Save button should not be visible after successful save.");
        }
    }
}
