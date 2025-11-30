using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{

    /// <summary>
    /// Represents crt-datetimepicker controls: Time, Date and DateTime.
    /// </summary>
    public sealed class DateTimeField : BaseField
    {
        public DateTimeFieldTypeEnum DateTimeType { get; }

        protected override string FieldTypeName => "DateTimeField";

        public DateTimeField(
            CreatioPage page,
            string title,
            string code,
            bool required,
            bool readOnly,
            string? placeholder,
            DateTimeFieldTypeEnum dateTimeType)
            : base(page, title, code, readOnly, required, placeholder)
        {
            DateTimeType = dateTimeType;
        }

        protected override string BuildContainerSelector()
        {
            var code = Code;

            return
                $"crt-datetimepicker[element-name=\"{code}\"], " +
                $"crt-datetimepicker[id=\"{code}\"]";
        }

        protected override ILocator GetValueLocator(ILocator root)
        {
            return root.Locator("input.mat-input-element");
        }

        private string FormatDateTime(DateTime value)
        {
            switch (DateTimeType)
            {
                case DateTimeFieldTypeEnum.Time:
                    // Example: 7:36 AM
                    return value.ToString("h:mm tt", CultureInfo.InvariantCulture);

                case DateTimeFieldTypeEnum.Date:
                    // Example: 11/30/2025
                    return value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                case DateTimeFieldTypeEnum.DateTime:
                default:
                    // Example: 11/30/2025 7:36 AM
                    return value.ToString("MM/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Parse raw string from UI and normalize value according to DateTimeType:
        /// Time  -> 0001-01-01 HH:mm:00
        /// Date  -> yyyy-MM-dd 00:00:00
        /// DateTime -> full date+time as parsed.
        /// </summary>
        private DateTime? ParseDateTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();

            string[] patterns;
            switch (DateTimeType)
            {
                case DateTimeFieldTypeEnum.Time:
                    patterns = new[] { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
                    break;

                case DateTimeFieldTypeEnum.Date:
                    patterns = new[] { "MM/dd/yyyy" };
                    break;

                case DateTimeFieldTypeEnum.DateTime:
                default:
                    patterns = new[] { "MM/dd/yyyy h:mm tt", "MM/dd/yyyy hh:mm tt" };
                    break;
            }

            DateTime? parsed = null;

            // Exact parse first
            foreach (var pattern in patterns)
            {
                if (DateTime.TryParseExact(
                        raw,
                        pattern,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var dtExact))
                {
                    parsed = dtExact;
                    break;
                }
            }

            // Fallback general parse (на случай локали пользователя)
            if (!parsed.HasValue &&
                DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var dtGeneral))
            {
                parsed = dtGeneral;
            }

            if (!parsed.HasValue)
            {
                return null;
            }

            var dt = parsed.Value;

            // Нормализация по типу поля
            switch (DateTimeType)
            {
                case DateTimeFieldTypeEnum.Time:
                    // Дата всегда MinValue, берём только время (часы/минуты/секунды)
                    return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Unspecified);

                case DateTimeFieldTypeEnum.Date:
                    // Только дата, время = 00:00:00
                    return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Unspecified);

                case DateTimeFieldTypeEnum.DateTime:
                default:
                    return dt;
            }
        }

        /// <summary>
        /// Set value using raw string (already in UI format).
        /// </summary>
        public async Task SetValueAsync(string value, bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);

            try
            {
                await input.FillAsync(value ?? string.Empty).ConfigureAwait(false);
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync(string) '{Title}' (Code='{Code}') = '{value}', Type={DateTimeType}.");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync(string) Playwright error for '{Title}' (Code='{Code}'): {ex.Message}");
                }

                throw;
            }
        }

        public void SetValue(string value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set value using DateTime. Value is formatted according to DateTimeType:
        /// Time: "h:mm tt", Date: "MM/dd/yyyy", DateTime: "MM/dd/yyyy h:mm tt".
        /// </summary>
        public async Task SetValueAsync(DateTime value, bool debug = false)
        {
            var formatted = FormatDateTime(value);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] SetValueAsync(DateTime) '{Title}' (Code='{Code}') raw={value:o}, formatted='{formatted}', Type={DateTimeType}.");
            }

            await SetValueAsync(formatted, debug).ConfigureAwait(false);
        }

        public void SetValue(DateTime value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get current value as DateTime?.
        /// Time: 0001-01-01 HH:mm:00
        /// Date: yyyy-MM-dd 00:00:00
        /// DateTime: полная дата+время.
        /// </summary>
        public async Task<DateTime?> GetValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            var raw = await input.InputValueAsync().ConfigureAwait(false);
            var parsed = ParseDateTime(raw);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetValueAsync '{Title}' (Code='{Code}') raw='{raw}', parsed={parsed?.ToString("o") ?? "null"}, Type={DateTimeType}.");
            }

            return parsed;
        }

        public DateTime? GetValue(bool debug = false)
        {
            return GetValueAsync(debug).GetAwaiter().GetResult();
        }
    }
}