using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Base class for all Creatio Freedom UI fields.
    /// Contains common logic of locating the field container,
    /// checking read-only / required / placeholder and logging.
    /// </summary>
    public abstract class BaseField : IField
    {
        private static readonly int[] DefaultTimeoutsMs = { 10_000, 20_000, 30_000 };

        /// <summary>
        /// Associated Creatio page wrapper.
        /// </summary>
        public CreatioPage Page { get; }

        public string Title { get; }

        public string Code { get; }

        public bool ReadOnly { get; protected set; }

        public bool Required { get; protected set; }

        public string? Placeholder { get; protected set; }

        /// <summary>
        /// Cached root locator of the field container (crt-input, crt-checkbox, etc.).
        /// Once located successfully it is reused for all subsequent checks.
        /// </summary>
        protected ILocator? CachedRoot { get; private set; }

        /// <summary>
        /// Short type name for logging, for example "TextField", "NumberField".
        /// </summary>
        protected abstract string FieldTypeName { get; }

        protected BaseField(
            CreatioPage page,
            string title,
            string code,
            bool readOnly,
            bool required,
            string? placeholder)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title must be provided.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Code must be provided.", nameof(code));
            }

            Title = title;
            Code = code;
            ReadOnly = readOnly;
            Required = required;
            Placeholder = placeholder;
        }

        /// <summary>
        /// Reset cached root locator (for example after page reload).
        /// </summary>
        protected void ResetCache()
        {
            CachedRoot = null;
        }

        /// <summary>
        /// Build selector for field container.
        /// Derived classes provide concrete implementation with proper tags.
        /// </summary>
        protected abstract string BuildContainerSelector();

        /// <summary>
        /// Return locator for field label inside the container.
        /// </summary>
        protected virtual ILocator GetLabelLocator(ILocator root)
        {
            return root.Locator(".crt-input-label");
        }

        /// <summary>
        /// Return locator for "value" element (input / textarea) inside the container.
        /// Text/number/datetime fields override appropriately if needed.
        /// </summary>
        protected virtual ILocator GetValueLocator(ILocator root)
        {
            return root.Locator("input, textarea");
        }

        /// <summary>
        /// Try to find the field container on the page.
        /// Uses multiple timeouts and matches by label.
        /// Result is cached in <see cref="CachedRoot"/>.
        /// </summary>
        protected async Task<ILocator?> FindFieldContainerAsync(bool debug = false, int? timeoutOverrideMs = null)
        {
            if (Page.Page == null)
            {
                throw new InvalidOperationException("CreatioPage is not initialized. Call InitializeAsync() first.");
            }

            if (CachedRoot != null)
            {
                return CachedRoot;
            }

            var selector = BuildContainerSelector();
            var candidates = Page.Page.Locator(selector);

            var timeouts = timeoutOverrideMs.HasValue
                ? new[] { timeoutOverrideMs.Value }
                : DefaultTimeoutsMs;

            var attempt = 0;
            foreach (var timeout in timeouts)
            {
                attempt++;

                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] Attempt {attempt} to find field '{Title}' (Code='{Code}') with timeout {timeout} ms.");
                }

                try
                {
                    await candidates.First.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = timeout
                    }).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] Attempt {attempt} timed out.");
                    }
                    continue;
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] Attempt {attempt} Playwright error: {ex.Message}");
                    }
                    continue;
                }

                var count = await candidates.CountAsync().ConfigureAwait(false);
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] Total field containers on page: {count}");
                    for (var i = 0; i < count; i++)
                    {
                        var container = candidates.Nth(i);
                        var id = await container.GetAttributeAsync("id").ConfigureAwait(false);
                        var elementName = await container.GetAttributeAsync("element-name").ConfigureAwait(false);

                        var labelLoc = GetLabelLocator(container);
                        var labelText = string.Empty;
                        if (await labelLoc.CountAsync().ConfigureAwait(false) > 0)
                        {
                            labelText = await labelLoc.First.InnerTextAsync().ConfigureAwait(false);
                        }

                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}]  #{i}: id='{id}', element-name='{elementName}', label='{labelText}'");
                    }
                }

                var matchedRoot = await FindMatchingContainerByLabelAsync(candidates, debug).ConfigureAwait(false);
                if (matchedRoot != null)
                {
                    CachedRoot = matchedRoot;
                    return CachedRoot;
                }
            }

            return null;
        }

        private async Task<ILocator?> FindMatchingContainerByLabelAsync(ILocator candidates, bool debug)
        {
            var count = await candidates.CountAsync().ConfigureAwait(false);
            var expected = NormalizeLabelText(Title);

            for (var i = 0; i < count; i++)
            {
                var container = candidates.Nth(i);
                var labelLoc = GetLabelLocator(container);
                var labelCount = await labelLoc.CountAsync().ConfigureAwait(false);
                if (labelCount <= 0)
                {
                    continue;
                }

                var rawText = await labelLoc.First.InnerTextAsync().ConfigureAwait(false);
                var actual = NormalizeLabelText(rawText);
                var equal = string.Equals(actual, expected, StringComparison.Ordinal);

                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] Label match. Expected='{expected}', Actual='{actual}', Result={equal}");
                }

                if (equal)
                {
                    return container;
                }
            }

            return null;
        }

        public async Task<bool> CheckIfExistAsync(bool debug = false, int? timeoutOverrideMs = null)
        {
            var root = await FindFieldContainerAsync(debug, timeoutOverrideMs).ConfigureAwait(false);
            return root != null;
        }

        public bool CheckIfExist(bool debug = false, int? timeoutOverrideMs = null)
        {
            return CheckIfExistAsync(debug, timeoutOverrideMs).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Detect read-only state using common Freedom UI rules.
        /// Derived classes may override for specific control types.
        /// </summary>
        protected virtual async Task<bool> DetectReadOnlyAsync(ILocator root, bool debug)
        {
            var input = GetValueLocator(root);

            var rootReadonly = await root.GetAttributeAsync("readonly").ConfigureAwait(false);
            var inputReadonly = await input.GetAttributeAsync("readonly").ConfigureAwait(false);
            var ariaReadonly = await input.GetAttributeAsync("aria-readonly").ConfigureAwait(false);
            var disabled = await input.IsDisabledAsync().ConfigureAwait(false);

            var lockIcon = root.Locator(".readonly-icon");
            var lockIconCount = await lockIcon.CountAsync().ConfigureAwait(false);

            var hasRootReadonly = !string.IsNullOrEmpty(rootReadonly) &&
                                  !string.Equals(rootReadonly, "false", StringComparison.OrdinalIgnoreCase);
            var hasInputReadonly = !string.IsNullOrEmpty(inputReadonly) &&
                                   !string.Equals(inputReadonly, "false", StringComparison.OrdinalIgnoreCase);
            var ariaFlag = string.Equals(ariaReadonly, "true", StringComparison.OrdinalIgnoreCase);

            var detected = hasRootReadonly || hasInputReadonly || ariaFlag || disabled || lockIconCount > 0;

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] DetectReadOnlyAsync: rootReadonly='{rootReadonly}', " +
                    $"inputReadonly='{inputReadonly}', ariaReadonly='{ariaReadonly}', disabled={disabled}, " +
                    $"lockIconCount={lockIconCount}, Result={detected}");
            }

            return detected;
        }

        public virtual async Task<bool> CheckIfReadOnlyAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckIfReadOnlyAsync: field '{Title}' (Code='{Code}') not found.");
                }
                return false;
            }

            var detected = await DetectReadOnlyAsync(root, debug).ConfigureAwait(false);
            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckIfReadOnlyAsync: Expected={ReadOnly}, Detected={detected}");
            }

            return detected == ReadOnly;
        }

        public bool CheckIfReadOnly(bool debug = false)
        {
            return CheckIfReadOnlyAsync(debug).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Detect required state using common Freedom UI rules.
        /// Derived classes may override.
        /// </summary>
        protected virtual async Task<bool> DetectRequiredAsync(ILocator root, bool debug)
        {
            var input = GetValueLocator(root);

            var requiredAttr = await input.GetAttributeAsync("required").ConfigureAwait(false);
            var ariaRequired = await input.GetAttributeAsync("aria-required").ConfigureAwait(false);
            var classAttr = await input.GetAttributeAsync("class").ConfigureAwait(false) ?? string.Empty;

            var labelLoc = GetLabelLocator(root);
            var labelClass = string.Empty;
            if (await labelLoc.CountAsync().ConfigureAwait(false) > 0)
            {
                labelClass = await labelLoc.First.GetAttributeAsync("class").ConfigureAwait(false) ?? string.Empty;
            }

            var hasRequiredAttr = !string.IsNullOrEmpty(requiredAttr) &&
                                  !string.Equals(requiredAttr, "false", StringComparison.OrdinalIgnoreCase);
            var ariaFlag = string.Equals(ariaRequired, "true", StringComparison.OrdinalIgnoreCase);
            var inputClassFlag = classAttr.IndexOf("required", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 classAttr.IndexOf("required-input", StringComparison.OrdinalIgnoreCase) >= 0;
            var labelFlag = labelClass.IndexOf("crt-input-required", StringComparison.OrdinalIgnoreCase) >= 0;

            var detected = hasRequiredAttr || ariaFlag || inputClassFlag || labelFlag;

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] DetectRequiredAsync: requiredAttr='{requiredAttr}', " +
                    $"ariaRequired='{ariaRequired}', inputClass='{classAttr}', labelClass='{labelClass}', " +
                    $"Result={detected}");
            }

            return detected;
        }

        public virtual async Task<bool> CheckIfRequiredAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckIfRequiredAsync: field '{Title}' (Code='{Code}') not found.");
                }
                return false;
            }

            var detected = await DetectRequiredAsync(root, debug).ConfigureAwait(false);
            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckIfRequiredAsync: Expected={Required}, Detected={detected}");
            }

            return detected == Required;
        }

        public bool CheckIfRequired(bool debug = false)
        {
            return CheckIfRequiredAsync(debug).GetAwaiter().GetResult();
        }

        public virtual async Task<bool> CheckPlaceholderAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckPlaceholderAsync: field '{Title}' (Code='{Code}') not found.");
                }
                return false;
            }

            var input = GetValueLocator(root);

            var dataPlaceholder = await input.GetAttributeAsync("data-placeholder").ConfigureAwait(false);
            var placeholderAttr = await input.GetAttributeAsync("placeholder").ConfigureAwait(false);

            // Raw values, что реально в DOM
            var actualRaw = string.IsNullOrWhiteSpace(dataPlaceholder) ? placeholderAttr : dataPlaceholder;
            var expectedRaw = Placeholder;

            // Нормализуем: null/""/пробелы → null
            string? actual = string.IsNullOrWhiteSpace(actualRaw) ? null : actualRaw.Trim();
            string? expected = string.IsNullOrWhiteSpace(expectedRaw) ? null : expectedRaw.Trim();

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckPlaceholderAsync: Expected='{expected ?? "null"}', Actual='{actual ?? "null"}'");
            }
            if (expected == null)
            {
                return actual == null;
            }
            return string.Equals(actual, expected, StringComparison.Ordinal);
        }


        public bool CheckPlaceholder(bool debug = false)
        {
            return CheckPlaceholderAsync(debug).GetAwaiter().GetResult();
        }

        public virtual async Task<bool> CheckFieldAsync(bool debug = false, int? timeoutOverrideMs = null)
        {
            var exists = await CheckIfExistAsync(debug, timeoutOverrideMs).ConfigureAwait(false);
            if (!exists)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckFieldAsync: field '{Title}' (Code='{Code}') does not exist.");
                }
                return false;
            }

            var readOnlyOk = await CheckIfReadOnlyAsync(debug).ConfigureAwait(false);
            if (!readOnlyOk)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckFieldAsync: read-only check failed.");
                }
                return false;
            }

            var requiredOk = await CheckIfRequiredAsync(debug).ConfigureAwait(false);
            if (!requiredOk)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckFieldAsync: required check failed.");
                }
                return false;
            }

            var placeholderOk = await CheckPlaceholderAsync(debug).ConfigureAwait(false);
            if (!placeholderOk)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] CheckFieldAsync: placeholder check failed.");
                }
                return false;
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] CheckFieldAsync: all checks passed.");
            }

            return true;
        }

        public bool CheckField(bool debug = false, int? timeoutOverrideMs = null)
        {
            return CheckFieldAsync(debug, timeoutOverrideMs).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Normalize label text: trim spaces, remove trailing " *" or ":".
        /// </summary>
        protected static string NormalizeLabelText(string? text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            trimmed = trimmed.TrimEnd(' ', '*', ':');
            return trimmed.Trim();
        }
    }
}