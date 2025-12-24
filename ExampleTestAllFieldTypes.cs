using System;
using System.Threading.Tasks;
using CreatioAutoTestsPlaywright.Environment;
using CreatioAutoTestsPlaywright.Frontend;
using Microsoft.Playwright;
using NUnit.Framework;

namespace CreatioAutoTestsPlaywright
{
    /// <summary>
    /// Tests that verify value roundtrip (Set/Get) for every field type and subtype
    /// on a dedicated Creatio Freedom UI test page.
    /// These tests do NOT use pages.config.json and rely on hard-coded field codes.
    /// </summary>
    [TestFixture]
    public sealed class FieldTypesRoundtripTests : BaseCreatioTest
    {
        protected override string WorkingDirectoryPath =>
            "C:\\Users\\aivzhenko\\source\\repos\\CreatioAutoTestsPlaywright\\";
        protected override string CreatioEnvConfigJson =>
            "dev.creatio.env.config.json";

        /// <summary>
        /// Underlying Creatio page used for all field roundtrip tests.
        /// </summary>
        public CreatioPage Page = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUpPage()
        {
            // Use the same test page as in DevCreatioTestExample.
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

        // ----------------------------
        // TEXT FIELD ROUNDTRIP TESTS
        // ----------------------------

        /// <summary>
        /// Verifies that a simple TextField (Text subtype, e.g. Name)
        /// can successfully set and retrieve a plain string value.
        /// </summary>
        [Test]
        public async Task TextField_Text_Subtype_SetAndGetValue_Works()
        {
            var field = new TextField(
                page: Page,
                title: "Name",
                code: "Input_zsrmb1i",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Text);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Name text field should exist and match basic properties.");

            const string valueToSet = "Sample Name 123";
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet), "Text field value roundtrip (Text subtype) failed.");
        }

        /// <summary>
        /// Verifies that a RichText TextField can set and retrieve plain text content.
        /// HTML formatting is ignored at this level; only text roundtrip is validated.
        /// </summary>
        [Test]
        public async Task TextField_RichText_Subtype_SetAndGetValue_Works()
        {
            var field = new TextField(
                page: Page,
                title: "RichText",
                code: "RichTextEditor_46xg6fy",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.RichText
            );

            var ok = await field.CheckFieldAsync(debug: true);

            Assert.That(ok, Is.True, "RichText field should exist and match basic properties.");

            const string valueToSet = "Rich text content: line 1\nline 2";
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet + "1"), "Text field value roundtrip (RichText subtype) failed.");
        }

        /// <summary>
        /// Verifies that a Link TextField can set and retrieve a URL.
        /// </summary>
        [Test]
        public async Task TextField_Link_Subtype_SetAndGetValue_Works()
        {
            var field = new TextField(
                page: Page,
                title: "Link",
                code: "WebInput_nwd7272",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Link);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Link field should exist and match basic properties.");

            const string valueToSet = "https://example.com/path?q=1";
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet), "Text field value roundtrip (Link subtype) failed.");
        }

        /// <summary>
        /// Verifies that an Email TextField can set and retrieve an email address.
        /// </summary>
        [Test]
        public async Task TextField_Email_Subtype_SetAndGetValue_Works()
        {
            var field = new TextField(
                page: Page,
                title: "Email",
                code: "EmailInput_int43pb",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Email);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Email field should exist and match basic properties.");

            const string valueToSet = "qa@example.com";
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet), "Text field value roundtrip (Email subtype) failed.");
        }

        /// <summary>
        /// Verifies that a PhoneNumber TextField can set and retrieve a phone number.
        /// </summary>
        [Test]
        public async Task TextField_PhoneNumber_Subtype_SetAndGetValue_Works()
        {
            var field = new TextField(
                page: Page,
                title: "PhoneNumber",
                code: "PhoneNumber",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.PhoneNumber);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "PhoneNumber field should exist and match basic properties.");

            const string valueToSet = "+380501112233";
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet), "Text field value roundtrip (PhoneNumber subtype) failed.");
        }

        // ----------------------------
        // DATETIME FIELD ROUNDTRIP TESTS
        // ----------------------------

        /// <summary>
        /// Verifies that a Time-only DateTimeField can set and retrieve a time value,
        /// normalizing date part to 0001-01-01 as per field implementation.
        /// </summary>
        [Test]
        public async Task DateTimeField_Time_Subtype_SetAndGetValue_Works()
        {
            var field = new DateTimeField(
                page: Page,
                title: "Time",
                code: "DateTimePicker_trvp85o",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Time);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Time field should exist and match basic properties.");

            var timeToSet = new DateTime(2000, 1, 1, 7, 36, 0);
            await field.SetValueAsync(timeToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.Not.Null, "Time value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(value!.Value.Year, Is.EqualTo(1), "Time.Year should be normalized to 1.");
                Assert.That(value.Value.Month, Is.EqualTo(1), "Time.Month should be normalized to 1.");
                Assert.That(value.Value.Day, Is.EqualTo(1), "Time.Day should be normalized to 1.");
                Assert.That(value.Value.Hour, Is.EqualTo(timeToSet.Hour), "Time.Hour mismatch.");
                Assert.That(value.Value.Minute, Is.EqualTo(timeToSet.Minute), "Time.Minute mismatch.");
            });
        }

        /// <summary>
        /// Verifies that a Date-only DateTimeField can set and retrieve date,
        /// normalizing time part to 00:00:00.
        /// </summary>
        [Test]
        public async Task DateTimeField_Date_Subtype_SetAndGetValue_Works()
        {
            var field = new DateTimeField(
                page: Page,
                title: "Date",
                code: "DateTimePicker_2ptldop",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Date);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Date field should exist and match basic properties.");

            var dateToSet = new DateTime(2025, 11, 30, 15, 30, 0);
            await field.SetValueAsync(dateToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.Not.Null, "Date value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(value!.Value.Year, Is.EqualTo(2025), "Date.Year mismatch.");
                Assert.That(value.Value.Month, Is.EqualTo(11), "Date.Month mismatch.");
                Assert.That(value.Value.Day, Is.EqualTo(30), "Date.Day mismatch.");
                Assert.That(value.Value.Hour, Is.EqualTo(0), "Date.Hour should be 0.");
                Assert.That(value.Value.Minute, Is.EqualTo(0), "Date.Minute should be 0.");
                Assert.That(value.Value.Second, Is.EqualTo(0), "Date.Second should be 0.");
            });
        }

        /// <summary>
        /// Verifies that a DateTime DateTimeField can set and retrieve both date
        /// and time parts without normalization.
        /// </summary>
        [Test]
        public async Task DateTimeField_DateTime_Subtype_SetAndGetValue_Works()
        {
            var field = new DateTimeField(
                page: Page,
                title: "DateTime",
                code: "DateTimePicker_743h6kw",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.DateTime);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "DateTime field should exist and match basic properties.");

            var dateTimeToSet = new DateTime(2025, 11, 30, 7, 35, 0);
            await field.SetValueAsync(dateTimeToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.Not.Null, "DateTime value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(value!.Value.Year, Is.EqualTo(2025), "DateTime.Year mismatch.");
                Assert.That(value.Value.Month, Is.EqualTo(11), "DateTime.Month mismatch.");
                Assert.That(value.Value.Day, Is.EqualTo(30), "DateTime.Day mismatch.");
                Assert.That(value.Value.Hour, Is.EqualTo(7), "DateTime.Hour mismatch.");
                Assert.That(value.Value.Minute, Is.EqualTo(35), "DateTime.Minute mismatch.");
            });
        }

        // ----------------------------
        // NUMBER FIELD ROUNDTRIP TESTS
        // ----------------------------

        /// <summary>
        /// Verifies that an Integer NumberField can set and retrieve an integer value.
        /// </summary>
        [Test]
        public async Task NumberField_Integer_Subtype_SetAndGetValue_Works()
        {
            var field = new NumberField(
                page: Page,
                title: "Integer",
                code: "NumberInput_to84api",
                required: false,
                readOnly: false,
                placeholder: null,
                numberType: NumberFieldTypeEnum.Integer);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Integer field should exist and match basic properties.");

            const int valueToSet = 123;
            await field.SetValueAsync(valueToSet, debug: true);
            var value = await field.GetValueAsync(debug: true);

            Assert.That(value, Is.EqualTo(valueToSet), "Number field value roundtrip (Integer subtype) failed.");
        }

        // Когда на странице появится отдельное decimal-поле, можно будет добавить аналогичный тест:
        // NumberField_Decimal_Subtype_SetAndGetValue_Works()

        // ----------------------------
        // BOOLEAN FIELD ROUNDTRIP TESTS
        // ----------------------------

        /// <summary>
        /// Verifies that a BooleanField can toggle between true and false
        /// and correctly returns its state.
        /// </summary>
        [Test]
        public async Task BooleanField_SetAndGetValue_Works()
        {
            var field = new BooleanField(
                page: Page,
                title: "Boolean",
                code: "Checkbox_3qmzec5",
                readOnly: false);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Boolean field should exist and match basic properties.");

            await field.SetValueAsync(true, debug: true);
            var value = await field.GetValueAsync(debug: true);
            Assert.That(value, Is.True, "Boolean field should be true after SetValueAsync(true).");

            await field.SetValueAsync(false, debug: true);
            value = await field.GetValueAsync(debug: true);
            Assert.That(value, Is.False, "Boolean field should be false after SetValueAsync(false).");
        }

        // ----------------------------
        // LOOKUP FIELD ROUNDTRIP TESTS
        // ----------------------------

        /// <summary>
        /// Verifies that a LookupField can set and retrieve several different options by text:
        /// SysPortalConnection, Creatio.ai, Creatio Maintenance.
        /// </summary>
        [Test]
        public async Task LookupField_SetAndGetValue_Works_ForContact()
        {
            var field = new LookupField(
                page: Page,
                title: "Contact",
                code: "ComboBox_gbg5vsp",
                required: false,
                readOnly: false,
                placeholder: null);

            var ok = await field.CheckFieldAsync(debug: true);
            Assert.That(ok, Is.True, "Contact lookup field should exist and match basic properties.");

            // 1) SysPortalConnection
            await field.SetValueAsync("SysPortalConnection", debug: true);
            var value = await field.GetValueAsync(debug: true);
            Assert.That(value, Is.EqualTo("SysPortalConnection"),
                "Lookup field should be set to 'SysPortalConnection'.");

            await field.ClearValueAsync(debug: true);
            var cleared = await field.GetValueAsync(debug: true);
            Assert.That(cleared, Is.Null.Or.Empty, "Lookup field should be empty after ClearValueAsync().");

            // 2) Creatio.ai
            await field.SetValueAsync("Creatio.ai", debug: true);
            value = await field.GetValueAsync(debug: true);
            Assert.That(value, Is.EqualTo("Creatio.ai"),
                "Lookup field should be set to 'Creatio.ai'.");

            await field.ClearValueAsync(debug: true);
            cleared = await field.GetValueAsync(debug: true);
            Assert.That(cleared, Is.Null.Or.Empty, "Lookup field should be empty after ClearValueAsync().");

            // 3) Creatio Maintenance
            await field.SetValueAsync("Creatio Maintenance", debug: true);
            value = await field.GetValueAsync(debug: true);
            Assert.That(value, Is.EqualTo("Creatio Maintenance"),
                "Lookup field should be set to 'Creatio Maintenance'.");

            await field.ClearValueAsync(debug: true);
        }
    }
}
