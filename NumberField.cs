using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Represents crt-number-input controls for integer and decimal values.
    /// </summary>
    public sealed class NumberField : BaseField
    {
        public NumberFieldTypeEnum NumberType { get; }

        protected override string FieldTypeName => "NumberField";

        public NumberField(
            CreatioPage page,
            string title,
            string code,
            bool required,
            bool readOnly,
            string? placeholder,
            NumberFieldTypeEnum numberType)
            : base(page, title, code, readOnly, required, placeholder)
        {
            NumberType = numberType;
        }

        protected override string BuildContainerSelector()
        {
            var code = Code;

            return
                $"crt-number-input[element-name=\"{code}\"], " +
                $"crt-number-input[id=\"{code}\"]";
        }

        protected override ILocator GetValueLocator(ILocator root)
        {
            return root.Locator("input.mat-input-element");
        }

        public async Task SetValueAsync(int value, bool debug = false)
        {
            if (NumberType != NumberFieldTypeEnum.Integer)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') is not Integer type.");
            }

            await SetRawValueAsync(value.ToString(CultureInfo.InvariantCulture), debug)
                .ConfigureAwait(false);
        }

        public void SetValue(int value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }

        public async Task SetValueAsync(decimal value, bool debug = false)
        {
            if (NumberType != NumberFieldTypeEnum.Decimal)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') is not Decimal type.");
            }

            await SetRawValueAsync(value.ToString(CultureInfo.InvariantCulture), debug)
                .ConfigureAwait(false);
        }

        public void SetValue(decimal value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }

        private async Task SetRawValueAsync(string value, bool debug)
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
                        $"[Field:{FieldTypeName}] SetRawValueAsync '{Title}' (Code='{Code}') = '{value}', Type={NumberType}.");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetRawValueAsync Playwright error for '{Title}' (Code='{Code}'): {ex.Message}");
                }

                throw;
            }
        }

        private decimal? ParseNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Get current numeric value as decimal?.
        /// For integer fields the value will still be decimal but without fractional part.
        /// </summary>
        public async Task<decimal?> GetValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            var raw = await input.InputValueAsync().ConfigureAwait(false);
            var parsed = ParseNumber(raw);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetValueAsync '{Title}' (Code='{Code}') raw='{raw}', parsed={(parsed.HasValue ? parsed.Value.ToString(CultureInfo.InvariantCulture) : "null")}, Type={NumberType}.");
            }

            return parsed;
        }

        public decimal? GetValue(bool debug = false)
        {
            return GetValueAsync(debug).GetAwaiter().GetResult();
        }
    }
}