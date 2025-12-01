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
    //[Explicit("Run only when explicitly selected.")]
    public sealed class CreatioTestExample : BaseCreatioTest
    {
        protected override string WorkingDirectoryPath =>
            "Your working directory path\\source\\repos\\CreatioAutoTestsPlaywright\\";

        //// TODO: Update with your actual config file names
        protected override string CreatioEnvConfigJson => "creatio.env.config.json";
        protected override string CreatioSiteConfigJson => "creatio.site.config.json";

        private CreatioPage _page = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUpPage()
        {
            // TODO: Update path and username for your environment
            _page = new CreatioPage(
                "/0/Shell/#Card/TestPage_FormPage/edit/id",
                Env,
                Browser,
                "email@test.com");


            await _page.InitializeAsync(debug: false);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDownPage()
        {
            await _page.DisposeAsync();
        }

        #region Text Field Tests

        [Test]
        public async Task TestTextField_TextType_SetAndGetValue()
        {
            var textField = new TextField(
                page: _page,
                title: "Name",
                code: "Input_zsrmb1i",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Text);

            var testValue = "Sample text value";
            await textField.SetValueAsync(testValue, debug: false);
            var retrievedValue = await textField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.EqualTo(testValue));
        }
        [Test]
        public async Task TestTextFieldTextType()
        {
            // TODO: Update field parameters for your form
            var textField = new TextField(
                page: _page,
                title: "Name",
                code: "Input_zsrmb1i",
                required: true,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Text);

            var exists = await textField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Text field should exist on the page.");
        }

        [Test]
        public async Task TestTextField_EmailType()
        {
            var emailField = new TextField(
                page: _page,
                title: "Email",
                code: "EmailInput_int43pb",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Email);

            var exists = await emailField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Email field should exist on the page.");
        }

        [Test]
        public async Task TestTextField_LinkType()
        {
            var linkField = new TextField(
                page: _page,
                title: "Link",
                code: "WebInput_nwd7272",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.Link);

            var exists = await linkField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Link field should exist on the page.");
        }

        [Test]
        public async Task TestTextFieldPhoneNumberType()
        {
            var phoneField = new TextField(
                page: _page,
                title: "PhoneNumber",
                code: "PhoneNumber",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.PhoneNumber);

            var exists = await phoneField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Phone Number field should exist on the page.");
        }

        [Test]
        public async Task TestTextField_RichTextType()
        {
            var richTextField = new TextField(
                page: _page,
                title: "RichText",
                code: "RichTextEditor_46xg6fy",
                required: false,
                readOnly: false,
                placeholder: null,
                contentType: TextFieldTypeEnum.RichText);

            var exists = await richTextField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Rich Text field should exist on the page.");
        }
        #endregion

        #region Number Field Tests

        [Test]
        public async Task TestNumberField_IntegerType()
        {
            var integerField = new NumberField(
                page: _page,
                title: "Integer",
                code: "NumberInput_to84api",
                required: false,
                readOnly: false,
                placeholder: null,
                numberType: NumberFieldTypeEnum.Integer);

            var exists = await integerField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Integer field should exist on the page.");
        }

        [Test]
        public async Task TestNumberField_IntegerType_SetAndGetValue()
        {
            var integerField = new NumberField(
                page: _page,
                title: "Integer",
                code: "NumberInput_xyz",
                required: false,
                readOnly: false,
                placeholder: null,
                numberType: NumberFieldTypeEnum.Integer);

            int testValue = 12345;
            await integerField.SetValueAsync(testValue, debug: false);
            var retrievedValue = await integerField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.EqualTo(testValue));
        }

        [Test]
        public async Task TestNumberField_DecimalType()
        {
            var decimalField = new NumberField(
                page: _page,
                title: "Decimal",
                code: "NumberInput_decimal_xyz",
                required: false,
                readOnly: false,
                placeholder: null,
                numberType: NumberFieldTypeEnum.Decimal);

            var exists = await decimalField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Decimal field should exist on the page.");
        }

        [Test]
        public async Task TestNumberField_DecimalType_SetAndGetValue()
        {
            var decimalField = new NumberField(
                page: _page,
                title: "Decimal",
                code: "NumberInput_decimal_xyz",
                required: false,
                readOnly: false,
                placeholder: null,
                numberType: NumberFieldTypeEnum.Decimal);

            decimal testValue = 123.45m;
            await decimalField.SetValueAsync(testValue, debug: false);
            var retrievedValue = await decimalField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.EqualTo(testValue));
        }

        #endregion

        #region DateTime Field Tests

        [Test]
        public async Task TestDateTimeField_DateType()
        {
            var dateField = new DateTimeField(
                page: _page,
                title: "Date",
                code: "DateTimePicker_date_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Date);

            var exists = await dateField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Date field should exist on the page.");
        }

        [Test]
        public async Task TestDateTimeField_DateType_SetAndGetValue()
        {
            var dateField = new DateTimeField(
                page: _page,
                title: "Date",
                code: "DateTimePicker_date_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Date);

            var testDate = new DateTime(2025, 12, 25, 15, 30, 0);
            await dateField.SetValueAsync(testDate, debug: false);
            var retrievedValue = await dateField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.Not.Null, "Date value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(retrievedValue!.Value.Year, Is.EqualTo(testDate.Year), "Year mismatch.");
                Assert.That(retrievedValue.Value.Month, Is.EqualTo(testDate.Month), "Month mismatch.");
                Assert.That(retrievedValue.Value.Day, Is.EqualTo(testDate.Day), "Day mismatch.");
                Assert.That(retrievedValue.Value.Hour, Is.EqualTo(0), "Date hour should be 0.");
                Assert.That(retrievedValue.Value.Minute, Is.EqualTo(0), "Date minute should be 0.");
            });
        }

        [Test]
        public async Task TestDateTimeField_TimeType()
        {
            var timeField = new DateTimeField(
                page: _page,
                title: "Time",
                code: "DateTimePicker_time_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Time);

            var exists = await timeField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Time field should exist on the page.");
        }

        [Test]
        public async Task TestDateTimeField_TimeType_SetAndGetValue()
        {
            var timeField = new DateTimeField(
                page: _page,
                title: "Time",
                code: "DateTimePicker_time_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.Time);

            var testTime = new DateTime(2000, 1, 1, 14, 30, 0);
            await timeField.SetValueAsync(testTime, debug: false);
            var retrievedValue = await timeField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.Not.Null, "Time value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(retrievedValue!.Value.Year, Is.EqualTo(1), "Time year should be 1.");
                Assert.That(retrievedValue.Value.Month, Is.EqualTo(1), "Time month should be 1.");
                Assert.That(retrievedValue.Value.Day, Is.EqualTo(1), "Time day should be 1.");
                Assert.That(retrievedValue.Value.Hour, Is.EqualTo(testTime.Hour), "Hour mismatch.");
                Assert.That(retrievedValue.Value.Minute, Is.EqualTo(testTime.Minute), "Minute mismatch.");
            });
        }

        [Test]
        public async Task TestDateTimeField_DateTimeType()
        {
            var dateTimeField = new DateTimeField(
                page: _page,
                title: "DateTime",
                code: "DateTimePicker_datetime_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.DateTime);

            var exists = await dateTimeField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "DateTime field should exist on the page.");
        }

        [Test]
        public async Task TestDateTimeField_DateTimeType_SetAndGetValue()
        {
            var dateTimeField = new DateTimeField(
                page: _page,
                title: "DateTime",
                code: "DateTimePicker_datetime_xyz",
                required: false,
                readOnly: false,
                placeholder: string.Empty,
                dateTimeType: DateTimeFieldTypeEnum.DateTime);

            var testDateTime = new DateTime(2025, 12, 25, 14, 30, 0);
            await dateTimeField.SetValueAsync(testDateTime, debug: false);
            var retrievedValue = await dateTimeField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.Not.Null, "DateTime value should not be null.");
            Assert.Multiple(() =>
            {
                Assert.That(retrievedValue!.Value.Year, Is.EqualTo(testDateTime.Year), "Year mismatch.");
                Assert.That(retrievedValue.Value.Month, Is.EqualTo(testDateTime.Month), "Month mismatch.");
                Assert.That(retrievedValue.Value.Day, Is.EqualTo(testDateTime.Day), "Day mismatch.");
                Assert.That(retrievedValue.Value.Hour, Is.EqualTo(testDateTime.Hour), "Hour mismatch.");
                Assert.That(retrievedValue.Value.Minute, Is.EqualTo(testDateTime.Minute), "Minute mismatch.");
            });
        }

        #endregion

        #region Boolean Field Tests

        [Test]
        public async Task TestBooleanField()
        {
            var booleanField = new BooleanField(
                page: _page,
                title: "Boolean",
                code: "Checkbox_xyz",
                readOnly: false);

            var exists = await booleanField.CheckFieldAsync(debug: false);

            Assert.That(exists, Is.True, "Boolean field should exist on the page.");
        }

        [Test]
        public async Task TestBooleanField_SetTrueGetValue()
        {
            var booleanField = new BooleanField(
                page: _page,
                title: "Boolean",
                code: "Checkbox_xyz",
                readOnly: false);

            await booleanField.SetValueAsync(true, debug: false);
            var retrievedValue = await booleanField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.True, "Boolean value should be true.");
        }

        [Test]
        public async Task TestBooleanField_SetFalseGetValue()
        {
            var booleanField = new BooleanField(
                page: _page,
                title: "Boolean",
                code: "Checkbox_xyz",
                readOnly: false);

            await booleanField.SetValueAsync(false, debug: false);
            var retrievedValue = await booleanField.GetValueAsync(debug: false);

            Assert.That(retrievedValue, Is.False, "Boolean value should be false.");
        }

        #endregion
    }
}