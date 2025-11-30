using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Represents crt-checkbox controls (boolean fields).
    /// </summary>
    public sealed class BooleanField : BaseField
    {
        protected override string FieldTypeName => "BooleanField";

        public BooleanField(
            CreatioPage page,
            string title,
            string code,
            bool readOnly)
            : base(page, title, code, readOnly, required: false, placeholder: null)
        {
            Required = false;
        }

        protected override string BuildContainerSelector()
        {
            var code = Code;

            return
                $"crt-checkbox[element-name=\"{code}\"], " +
                $"crt-checkbox[id=\"{code}\"]";
        }

        protected override ILocator GetLabelLocator(ILocator root)
        {
            return root.Locator(".crt-checkbox-label");
        }

        protected override ILocator GetValueLocator(ILocator root)
        {
            return root.Locator("input.mat-checkbox-input");
        }

        protected override async Task<bool> DetectReadOnlyAsync(ILocator root, bool debug)
        {
            var input = GetValueLocator(root);
            var disabledAttr = await input.GetAttributeAsync("disabled").ConfigureAwait(false);
            var disabled = await input.IsDisabledAsync().ConfigureAwait(false);

            var classAttr = await input.GetAttributeAsync("class").ConfigureAwait(false) ?? string.Empty;

            var detected = disabled ||
                           !string.IsNullOrEmpty(disabledAttr) ||
                           classAttr.IndexOf("mat-checkbox-disabled", StringComparison.OrdinalIgnoreCase) >= 0;

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] DetectReadOnlyAsync: disabledAttr='{disabledAttr}', disabled={disabled}, class='{classAttr}', Result={detected}");
            }

            return detected;
        }

        public override Task<bool> CheckIfRequiredAsync(bool debug = false)
        {
            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckIfRequiredAsync: Boolean fields are never required. Expected=False.");
            }

            return Task.FromResult(true);
        }

        public override Task<bool> CheckPlaceholderAsync(bool debug = false)
        {
            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckPlaceholderAsync: Boolean fields do not use placeholders.");
            }

            return Task.FromResult(true);
        }

        public async Task<bool> GetValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            var isChecked = await input.IsCheckedAsync().ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetValueAsync '{Title}' (Code='{Code}') = {isChecked}.");
            }

            return isChecked;
        }

        public bool GetValue(bool debug = false)
        {
            return GetValueAsync(debug).GetAwaiter().GetResult();
        }

        public async Task SetValueAsync(bool value, bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            var current = await input.IsCheckedAsync().ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] SetValueAsync '{Title}' (Code='{Code}') current={current}, target={value}");
            }

            if (current == value)
            {
                return;
            }

            ILocator clickTarget;

            var matCheckbox = root.Locator("mat-checkbox");
            if (await matCheckbox.CountAsync().ConfigureAwait(false) > 0)
            {
                clickTarget = matCheckbox.First;
            }
            else
            {
                var label = root.Locator("label.mat-checkbox-layout");
                clickTarget = (await label.CountAsync().ConfigureAwait(false) > 0)
                    ? label.First
                    : input;
            }

            try
            {
                await clickTarget.ClickAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync Playwright error for '{Title}' (Code='{Code}'): {ex.Message}");
                }
                throw;
            }

            var newValue = await input.IsCheckedAsync().ConfigureAwait(false);
            if (newValue != value)
            {
                var message =
                    $"Clicking the checkbox did not change its state for field '{Title}' (Code='{Code}'). " +
                    $"Expected={value}, Actual={newValue}";
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync error: {message}");
                }

                throw new InvalidOperationException(message);
            }
        }

        public void SetValue(bool value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }
    }
}