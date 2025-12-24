using System;
using System.Threading.Tasks;
using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Wrapper for Creatio Freedom UI button control (&lt;crt-button&gt;).
    /// Locates button by element-name/id (Code) and optional visible caption (Title).
    /// </summary>
    public sealed class Button : IButton
    {
        /// <summary>
        /// Parent Creatio page containing this button.
        /// </summary>
        public CreatioPage Page { get; }

        /// <summary>
        /// Human readable caption of the button (e.g. "Save").
        /// May be empty for icon-only buttons.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Technical button identifier equal to element-name / id on the &lt;crt-button&gt; host.
        /// </summary>
        public string Code { get; }

        private const int DefaultClickTimeoutMs = 10_000;

        public Button(CreatioPage page, string title, string code)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Title = title ?? string.Empty;
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        /// <summary>
        /// Builds base CSS selector for the root crt-button host.
        /// We support both element-name and id:
        /// &lt;crt-button element-name="SaveButton"&gt; or &lt;crt-button id="SaveButton"&gt;.
        /// </summary>
        private string BuildSelector()
        {
            return $"crt-button[element-name=\"{Code}\"] , crt-button#{Code}";
        }

        /// <summary>
        /// Returns locator for the root button host element (&lt;crt-button&gt;).
        /// </summary>
        private ILocator GetRootLocator()
        {
            var selector = BuildSelector();
            return Page.Page.Locator(selector);
        }

        /// <summary>
        /// Snapshot-style check: verifies that the button currently exists in DOM,
        /// is visible, and (if Title is not empty) that its caption matches Title
        /// (case-insensitive). No waiting, no timeout exceptions.
        /// </summary>
        public async Task<bool> CheckIfExistAsync(bool debug = false)
        {
            var selector = BuildSelector();
            var locator = Page.Page.Locator(selector);

            int count;
            try
            {
                count = await locator.CountAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Button] CheckIfExist: error counting elements for Title='{Title}', Code='{Code}', selector='{selector}': {ex.Message}");
                }
                return false;
            }

            if (count == 0)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Button] CheckIfExist: Title='{Title}', Code='{Code}', Exists=False (Count=0).");
                }
                return false;
            }

            var buttonRoot = locator.First;

            bool isVisible = false;
            try
            {
                isVisible = await buttonRoot.IsVisibleAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Button] CheckIfExist: IsVisibleAsync error for Title='{Title}', Code='{Code}': {ex.Message}");
                }
            }

            string? captionText = null;
            try
            {
                // Try to read visible caption text from common button caption containers.
                var captionLocator = buttonRoot.Locator(
                    ".crt-button-caption, .btn-reversible-content, .mdc-button__label, .mat-mdc-button-touch-target");

                var captionCount = await captionLocator.CountAsync().ConfigureAwait(false);
                if (captionCount > 0)
                {
                    captionText = (await captionLocator.First.InnerTextAsync().ConfigureAwait(false))?
                        .Trim();
                }
            }
            catch (PlaywrightException)
            {
                // Ignore caption errors, we will just rely on visibility.
            }

            bool titleMatches = true;
            if (!string.IsNullOrWhiteSpace(Title))
            {
                if (string.IsNullOrWhiteSpace(captionText))
                {
                    titleMatches = false;
                }
                else
                {
                    titleMatches = string.Equals(
                        Title.Trim(),
                        captionText.Trim(),
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Button] CheckIfExist: Title='{Title}', Code='{Code}', Exists={count > 0}, Visible={isVisible}, Caption='{captionText}', TitleMatches={titleMatches}.");
            }

            return count > 0 && isVisible && titleMatches;
        }

        public bool CheckIfExist(bool debug = false)
        {
            return CheckIfExistAsync(debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Clicks the button. If the button cannot be located or is disabled
        /// (aria-disabled/disabled), throws InvalidOperationException.
        /// Uses Playwright's click which waits for element to be visible and enabled.
        /// </summary>
        public async Task ClickAsync(bool debug = false)
        {
            var selector = BuildSelector();
            var locator = Page.Page.Locator(selector);

            // Ensure the button exists in DOM.
            var count = await locator.CountAsync().ConfigureAwait(false);
            if (count == 0)
            {
                throw new InvalidOperationException(
                    $"Button '{Title}' (Code='{Code}') not found on page (selector '{selector}').");
            }

            var buttonRoot = locator.First;

            // Resolve real clickable element inside crt-button.
            ILocator clickable = buttonRoot.Locator("button, .mdc-button, .mat-mdc-unelevated-button");
            var clickableCount = await clickable.CountAsync().ConfigureAwait(false);
            if (clickableCount == 0)
            {
                clickable = buttonRoot;
            }

            bool isDisabled = false;
            string? ariaDisabled = null;
            string? disabledAttr = null;

            try
            {
                ariaDisabled = await buttonRoot.GetAttributeAsync("aria-disabled").ConfigureAwait(false);
                disabledAttr = await buttonRoot.GetAttributeAsync("disabled").ConfigureAwait(false);

                isDisabled = string.Equals(ariaDisabled, "true", StringComparison.OrdinalIgnoreCase)
                             || disabledAttr != null;
            }
            catch (PlaywrightException)
            {
                // Ignore attribute errors, assume not disabled.
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Button] ClickAsync: Title='{Title}', Code='{Code}', IsDisabled={isDisabled}, aria-disabled='{ariaDisabled}', disabled='{disabledAttr}'.");
            }

            if (isDisabled)
            {
                throw new InvalidOperationException(
                    $"Button '{Title}' (Code='{Code}') is disabled and cannot be clicked.");
            }

            // Let Playwright handle wait for visible/enabled by using ClickAsync with timeout.
            await clickable.ClickAsync(new LocatorClickOptions
            {
                Timeout = DefaultClickTimeoutMs
            }).ConfigureAwait(false);

            if (debug)
            {
                FieldLogger.Write(
                    $"[Button] ClickAsync: clicked button '{Title}' (Code='{Code}').");
            }
        }

        public void Click(bool debug = false)
        {
            ClickAsync(debug).GetAwaiter().GetResult();
        }
    }
}