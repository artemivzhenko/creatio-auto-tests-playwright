using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{

    /// <summary>
    /// Represents text-like fields: crt-input, crt-rich-text-editor, crt-email-input,
    /// crt-phone-input, crt-web-input.
    /// </summary>
    public sealed class TextField : BaseField
    {
        public TextFieldTypeEnum ContentType { get; }

        protected override string FieldTypeName => "TextField";

        public TextField(
            CreatioPage page,
            string title,
            string code,
            bool required,
            bool readOnly,
            string? placeholder,
            TextFieldTypeEnum contentType)
            : base(page, title, code, readOnly, required, placeholder)
        {
            ContentType = contentType;
        }

        protected override string BuildContainerSelector()
        {
            var code = Code;

            switch (ContentType)
            {
                case TextFieldTypeEnum.RichText:
                    return
                        $"crt-rich-text-editor[element-name=\"{code}\"], " +
                        $"crt-rich-text-editor[id=\"{code}\"]";

                case TextFieldTypeEnum.Email:
                    return
                        $"crt-email-input[element-name=\"{code}\"], " +
                        $"crt-email-input[id=\"{code}\"]";

                case TextFieldTypeEnum.PhoneNumber:
                    return
                        $"crt-phone-input[element-name=\"{code}\"], " +
                        $"crt-phone-input[id=\"{code}\"]";

                case TextFieldTypeEnum.Link:
                    return
                        $"crt-web-input[element-name=\"{code}\"], " +
                        $"crt-web-input[id=\"{code}\"]";

                default:
                    return
                        $"crt-input[element-name=\"{code}\"], " +
                        $"crt-input[id=\"{code}\"]";
            }
        }

        protected override ILocator GetValueLocator(ILocator root)
        {
            switch (ContentType)
            {
                case TextFieldTypeEnum.RichText:
                    return root.Locator(".cke_textarea_inline");

                case TextFieldTypeEnum.Email:
                    // Editable email text input lives inside this container
                    return root.Locator(".crt-input-control-email-text input[data-qa=\"email-field-input\"]");

                case TextFieldTypeEnum.PhoneNumber:
                    return root.Locator("input[crtphoneinput], input.mat-input-element");

                case TextFieldTypeEnum.Link:
                    return root.Locator("input[data-qa=\"web-field-input\"], input.mat-input-element");

                default:
                    return root.Locator("input, textarea");
            }
        }

        /// <summary>
        /// Clear text value inside the field (if editable).
        /// For Email fields uses SetValueAsync with empty string to ensure proper state.
        /// </summary>
        /// <summary>
        /// Clear current field value in a way that is compatible with hidden inputs.
        /// For Email it will delegate to SetValueAsync("") to reuse email-specific logic.
        /// </summary>
        public async Task ClearValueAsync(bool debug = false)
        {
            // Email has its own mechanics (hidden actual input, "Add" button, etc.).
            if (ContentType == TextFieldTypeEnum.Email)
            {
                await SetValueAsync(string.Empty, debug).ConfigureAwait(false);
                return;
            }

            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);

            // First check if Playwright considers the input visible.
            bool isVisible = false;
            try
            {
                isVisible = await input.IsVisibleAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] ClearValueAsync: IsVisibleAsync failed for '{Title}' (Code='{Code}'): {ex.Message}");
                }
            }

            if (isVisible)
            {
                // Normal path: visible input, FillAsync should work fine.
                try
                {
                    await input.FillAsync(string.Empty).ConfigureAwait(false);
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: field '{Title}' (Code='{Code}') cleared via FillAsync.");
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: FillAsync failed for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                    throw;
                }
            }
            else
            {
                // Fallback: input exists but is hidden (e.g. crt-input-control-element-hidden).
                // Use JS to clear value and fire input event so Creatio reacts properly.
                try
                {
                    await input.EvaluateAsync(
                        @"(el) => {
                    if (!el) { return; }
                    el.value = '';
                    const evt = new Event('input', { bubbles: true, cancelable: true });
                    el.dispatchEvent(evt);
                }"
                    ).ConfigureAwait(false);

                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: field '{Title}' (Code='{Code}') cleared via JS fallback.");
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: JS fallback failed for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                    throw;
                }
            }
        }


        public void ClearValue(bool debug = false)
        {
            ClearValueAsync(debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set text value into the field.
        /// Email requires special handling because the actual input may be hidden
        /// until "add email" button is clicked.
        /// </summary>
        /// <summary>
        /// Set text value into the field.
        /// Email requires special handling because the actual input may be hidden
        /// until "add email" button is clicked.
        /// For other content types, if the input is hidden, JS fallback is used.
        /// </summary>
        public async Task SetValueAsync(string value, bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            // Special handling for Email fields (crt-email-input with "Add" button etc.).
            if (ContentType == TextFieldTypeEnum.Email)
            {
                await SetEmailValueInternalAsync(root, value, debug).ConfigureAwait(false);
                return;
            }

            var input = GetValueLocator(root);

            // Check visibility before we try FillAsync.
            bool isVisible = false;
            try
            {
                isVisible = await input.IsVisibleAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: IsVisibleAsync failed for '{Title}' (Code='{Code}'): {ex.Message}");
                }
            }

            if (isVisible)
            {
                // Normal path: visible input, use FillAsync.
                try
                {
                    await input.FillAsync(value ?? string.Empty).ConfigureAwait(false);
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] SetValueAsync '{Title}' (Code='{Code}') = '{value}' via FillAsync.");
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] SetValueAsync: FillAsync failed for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                    throw;
                }
            }
            else
            {
                // Fallback path: input exists but is not visible (Creatio phone/web, hidden input, etc.).
                // We set the value via JS and dispatch an 'input' event.
                try
                {
                    await input.EvaluateAsync(
                        @"(el, v) => {
                    if (!el) { return; }
                    el.value = v ?? '';
                    const evt = new Event('input', { bubbles: true, cancelable: true });
                    el.dispatchEvent(evt);
                }",
                        value ?? string.Empty
                    ).ConfigureAwait(false);

                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] SetValueAsync '{Title}' (Code='{Code}') = '{value}' via JS fallback (hidden input).");
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] SetValueAsync: JS fallback failed for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                    throw;
                }
            }
        }
        public void SetValue(string value, bool debug = false)
        {
            SetValueAsync(value, debug).GetAwaiter().GetResult();
        }

        private async Task SetEmailValueInternalAsync(ILocator root, string value, bool debug)
        {
            var emailTextContainer = root.Locator(".crt-input-control-email-text");
            var emailInput = emailTextContainer.Locator("input[data-qa=\"email-field-input\"]");
            var addButton = root.Locator("[data-qa=\"add-email-field-button\"]");

            // If editable email input is hidden, click "add email" button.
            var containerClass = await emailTextContainer.GetAttributeAsync("class").ConfigureAwait(false) ?? string.Empty;
            var isHidden = containerClass.IndexOf("crt-input-control-element-hidden", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isHidden)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetEmailValueInternalAsync: email input is hidden, clicking 'add email' button.");
                }

                if (await addButton.CountAsync().ConfigureAwait(false) > 0)
                {
                    await addButton.First.ClickAsync().ConfigureAwait(false);
                }

                await emailInput.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10_000
                }).ConfigureAwait(false);
            }

            try
            {
                await emailInput.FillAsync(value ?? string.Empty).ConfigureAwait(false);
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetEmailValueInternalAsync '{Title}' (Code='{Code}') = '{value}'.");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetEmailValueInternalAsync Playwright error for '{Title}' (Code='{Code}'): {ex.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Get current text value from the field.
        /// For Email fields tries editable input first; if it is hidden, falls back to link href/text.
        /// </summary>
        public async Task<string> GetValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            string value;

            if (ContentType == TextFieldTypeEnum.Email)
            {
                value = await GetEmailValueInternalAsync(root, debug).ConfigureAwait(false);
            }
            else if (ContentType == TextFieldTypeEnum.RichText)
            {
                var input = GetValueLocator(root);
                value = await input.InnerTextAsync().ConfigureAwait(false);
            }
            else
            {
                var input = GetValueLocator(root);
                value = await input.InputValueAsync().ConfigureAwait(false);
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetValueAsync '{Title}' (Code='{Code}') = '{value}'.");
            }

            return value;
        }

        public string GetValue(bool debug = false)
        {
            return GetValueAsync(debug).GetAwaiter().GetResult();
        }

        private async Task<string> GetEmailValueInternalAsync(ILocator root, bool debug)
        {
            var emailTextContainer = root.Locator(".crt-input-control-email-text");
            var emailInput = emailTextContainer.Locator("input[data-qa=\"email-field-input\"]");
            var link = root.Locator("a[data-qa=\"email-link-field-href\"]");

            var containerClass = await emailTextContainer.GetAttributeAsync("class").ConfigureAwait(false) ?? string.Empty;
            var isHidden = containerClass.IndexOf("crt-input-control-element-hidden", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isHidden)
            {
                var text = await emailInput.InputValueAsync().ConfigureAwait(false);
                return text;
            }

            if (await link.CountAsync().ConfigureAwait(false) == 0)
            {
                return string.Empty;
            }

            var href = await link.First.GetAttributeAsync("href").ConfigureAwait(false) ?? string.Empty;
            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                href = href.Substring("mailto:".Length);
            }

            var innerText = await link.First.InnerTextAsync().ConfigureAwait(false);
            var result = string.IsNullOrWhiteSpace(innerText) ? href : innerText.Trim();

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetEmailValueInternalAsync: href='{href}', innerText='{innerText}', result='{result}'.");
            }

            return result;
        }
    }
}
