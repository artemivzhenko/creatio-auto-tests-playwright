using CreatioAutoTestsPlaywright.Tools;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Freedom UI Lookup (crt-combobox) field wrapper for Creatio.
    /// </summary>
    public sealed class LookupField : BaseField
    {
        /// <summary>
        /// Timeout ожидания появления autocomplete-панели.
        /// </summary>
        private const int DropdownVisibleTimeoutMs = 10000;

        /// <summary>
        /// Максимальное время, в течение которого мы пытаемся дождаться нужной опции.
        /// </summary>
        private const int OptionResolveTimeoutMs = 3000;

        /// <summary>
        /// Небольшая пауза после клика по опции, чтобы DOM успел обновиться.
        /// </summary>
        private const int DefaultOptionWaitMs = 200;

        /// <summary>
        /// Интервал между повторными чтениями списка опций.
        /// </summary>
        private const int OptionPollIntervalMs = 100;

        protected override string FieldTypeName => "LookupField";

        public LookupField(
            CreatioPage page,
            string title,
            string code,
            bool required,
            bool readOnly,
            string? placeholder)
            : base(page, title, code, readOnly, required, placeholder)
        {
        }

        #region BaseField overrides

        protected override string BuildContainerSelector()
        {
            var code = Code;
            return
                $"crt-combobox[element-name=\"{code}\"] , " +
                $"crt-combobox[id=\"{code}\"]";
        }

        protected override ILocator GetLabelLocator(ILocator root)
        {
            return root.Locator(".crt-input-label");
        }

        protected override ILocator GetValueLocator(ILocator root)
        {
            return root.Locator("input.mat-input-element, input.crt-autocomplete-input, input[role='combobox']");
        }

        protected override async Task<bool> DetectReadOnlyAsync(ILocator root, bool debug)
        {
            var input = GetValueLocator(root);

            var rootReadonly = await root.GetAttributeAsync("readonly").ConfigureAwait(false);
            var inputReadonly = await input.GetAttributeAsync("readonly").ConfigureAwait(false);
            var ariaReadonly = await input.GetAttributeAsync("aria-readonly").ConfigureAwait(false);
            var disabled = await input.IsDisabledAsync().ConfigureAwait(false);

            var hasRootReadonly = !string.IsNullOrEmpty(rootReadonly) &&
                                  !string.Equals(rootReadonly, "false", StringComparison.OrdinalIgnoreCase);
            var hasInputReadonly = !string.IsNullOrEmpty(inputReadonly) &&
                                   !string.Equals(inputReadonly, "false", StringComparison.OrdinalIgnoreCase);
            var ariaFlag = string.Equals(ariaReadonly, "true", StringComparison.OrdinalIgnoreCase);

            var result = hasRootReadonly || hasInputReadonly || ariaFlag || disabled;

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] DetectReadOnlyAsync: rootReadonly='{rootReadonly}', " +
                    $"inputReadonly='{inputReadonly}', ariaReadonly='{ariaReadonly}', disabled={disabled}, Result={result}");
            }

            return result;
        }

        #endregion

        #region Helpers

        private static string SafeLog(string? text)
        {
            if (text == null)
            {
                return "<null>";
            }
            return text.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        /// <summary>
        /// Возвращает человекочитаемый текст из mat-option.
        /// </summary>
        private static async Task<string> GetOptionTextAsync(ILocator option)
        {
            // Пытаемся взять span[crttextoverflowtitle] (как в основном списке).
            var labelSpan = option.Locator(".mat-option-text span[crttextoverflowtitle]");
            try
            {
                if (await labelSpan.CountAsync().ConfigureAwait(false) > 0)
                {
                    var t = await labelSpan.First.InnerTextAsync().ConfigureAwait(false);
                    return t?.Trim() ?? string.Empty;
                }
            }
            catch
            {
                // fallthrough
            }

            // Fallback: весь .mat-option-text
            var matText = option.Locator(".mat-option-text");
            try
            {
                if (await matText.CountAsync().ConfigureAwait(false) > 0)
                {
                    var t = await matText.First.InnerTextAsync().ConfigureAwait(false);
                    return t?.Trim() ?? string.Empty;
                }
            }
            catch
            {
                // fallthrough
            }

            // Последний fallback — InnerText самого mat-option.
            try
            {
                var t = await option.InnerTextAsync().ConfigureAwait(false);
                return t?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class OptionInfo
        {
            public int Index { get; set; }
            public string? Id { get; set; }
            public string? AriaDisabled { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private static bool IsDisabled(OptionInfo o)
        {
            return string.Equals(o.AriaDisabled, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Служебные опции: "Add new", stickyBottomOption, addRecord_*.
        /// </summary>
        private static bool IsServiceOption(OptionInfo o)
        {
            if (!string.IsNullOrEmpty(o.Id))
            {
                if (o.Id.Equals("stickyBottomOption", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (o.Id.StartsWith("addRecord_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(o.Text) &&
                o.Text.TrimStart().StartsWith("Add new", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Get / Clear

        public async Task<string?> GetValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            string value = string.Empty;

            try
            {
                value = await input.InputValueAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] GetValueAsync: error reading input for '{Title}' (Code='{Code}'): {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                var trimmed = value.Trim();
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] GetValueAsync: '{Title}' (Code='{Code}') from input='{trimmed}'.");
                }
                return trimmed;
            }

            // Иногда выбранное значение рендерится как ссылка.
            var link = root.Locator(".link-wrap a.crt-link");
            var linkCount = await link.CountAsync().ConfigureAwait(false);
            if (linkCount > 0)
            {
                try
                {
                    var linkText = await link.First.InnerTextAsync().ConfigureAwait(false);
                    var trimmedLink = linkText?.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLink))
                    {
                        if (debug)
                        {
                            FieldLogger.Write(
                                $"[Field:{FieldTypeName}] GetValueAsync: '{Title}' (Code='{Code}') from link='{trimmedLink}'.");
                        }
                        return trimmedLink;
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] GetValueAsync: error reading link for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                }
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetValueAsync: '{Title}' (Code='{Code}') = null/empty.");
            }

            return null;
        }

        public string? GetValue(bool debug = false)
        {
            return GetValueAsync(debug).GetAwaiter().GetResult();
        }

        public async Task ClearValueAsync(bool debug = false)
        {
            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var clearBtn = root.Locator(".combobox-expander.clear, mat-icon[svgicon='small-close']");
            var clearCount = await clearBtn.CountAsync().ConfigureAwait(false);

            if (clearCount > 0)
            {
                try
                {
                    await clearBtn.First.ClickAsync().ConfigureAwait(false);
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: '{Title}' (Code='{Code}') cleared via clear icon.");
                    }
                    return;
                }
                catch (PlaywrightException ex)
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] ClearValueAsync: clear icon click failed for '{Title}' (Code='{Code}'): {ex.Message}");
                    }
                }
            }

            var input = GetValueLocator(root);
            try
            {
                await input.FillAsync(string.Empty).ConfigureAwait(false);
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] ClearValueAsync: '{Title}' (Code='{Code}') cleared via input.Fill(\"\").");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] ClearValueAsync: input.Fill failed for '{Title}' (Code='{Code}'): {ex.Message}");
                }
                throw;
            }
        }

        public void ClearValue(bool debug = false)
        {
            ClearValueAsync(debug).GetAwaiter().GetResult();
        }

        #endregion

        #region Set value

        public async Task SetValueAsync(string optionText, bool debug = false)
        {
            if (string.IsNullOrWhiteSpace(optionText))
            {
                throw new ArgumentException("Option text must be provided.", nameof(optionText));
            }

            if (Page.Page == null)
            {
                throw new InvalidOperationException("Page is not initialized.");
            }

            var page = Page.Page;
            var target = optionText.Trim();

            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] SetValueAsync: '{Title}' (Code='{Code}') attempting to set '{target}'.");
            }

            await ClearValueAsync(debug).ConfigureAwait(false);

            var input = GetValueLocator(root);
            try
            {
                await input.ClickAsync().ConfigureAwait(false);
                await input.FillAsync(target).ConfigureAwait(false);

                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: typed '{target}' into input for '{Title}' (Code='{Code}').");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: error typing into input for '{Title}' (Code='{Code}'): {ex.Message}");
                }
                throw;
            }

            // Ожидаем появления autocomplete-панели.
            var panelLocator = page.Locator("div.mat-autocomplete-panel.mat-autocomplete-visible");
            try
            {
                await panelLocator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = DropdownVisibleTimeoutMs
                }).ConfigureAwait(false);

                if (debug)
                {
                    var panelCount = await panelLocator.CountAsync().ConfigureAwait(false);
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: visible mat-autocomplete-panel count = {panelCount} for '{Title}' (Code='{Code}').");
                }
            }
            catch (PlaywrightException ex)
            {
                throw new InvalidOperationException(
                    $"Autocomplete dropdown did not appear for '{target}' in lookup field '{Title}' (Code='{Code}').",
                    ex);
            }

            var dropdown = panelLocator.First;

            // Ждём пока в панели появится подходящая опция (учёт асинхронной загрузки списка).
            var deadline = DateTime.UtcNow.AddMilliseconds(OptionResolveTimeoutMs);
            ILocator? match = null;

            int iteration = 0;
            while (true)
            {
                iteration++;

                var options = dropdown.Locator("mat-option");
                var optionCount = await options.CountAsync().ConfigureAwait(false);

                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: iteration={iteration}, options count={optionCount} for '{Title}' (Code='{Code}').");
                }

                for (var i = 0; i < optionCount; i++)
                {
                    var optLocator = options.Nth(i);

                    string? id = null;
                    string? ariaDisabled = null;
                    try { id = await optLocator.GetAttributeAsync("id").ConfigureAwait(false); } catch { }
                    try { ariaDisabled = await optLocator.GetAttributeAsync("aria-disabled").ConfigureAwait(false); } catch { }

                    var text = await GetOptionTextAsync(optLocator).ConfigureAwait(false);

                    var info = new OptionInfo
                    {
                        Index = i,
                        Id = id,
                        AriaDisabled = ariaDisabled,
                        Text = text
                    };

                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] SetValueAsync: Option #{i}: id='{info.Id}', aria-disabled='{info.AriaDisabled}', text='{SafeLog(info.Text)}'.");
                    }

                    if (IsDisabled(info) || IsServiceOption(info))
                    {
                        continue;
                    }

                    if (string.Equals(info.Text, target, StringComparison.Ordinal) ||
                        string.Equals(info.Text, target, StringComparison.OrdinalIgnoreCase))
                    {
                        match = optLocator;
                        if (debug)
                        {
                            FieldLogger.Write(
                                $"[Field:{FieldTypeName}] SetValueAsync: MATCH option #{i} text='{info.Text}' for target '{target}'.");
                        }
                        break;
                    }
                }

                if (match != null)
                {
                    break;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    break;
                }

                await page.WaitForTimeoutAsync(OptionPollIntervalMs).ConfigureAwait(false);
            }

            if (match == null)
            {
                throw new InvalidOperationException(
                    $"Option '{optionText}' not found in lookup field '{Title}' (Code='{Code}') " +
                    $"within {OptionResolveTimeoutMs} ms after dropdown appeared.");
            }

            try
            {
                await match.ClickAsync().ConfigureAwait(false);
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: clicked option '{target}' for field '{Title}' (Code='{Code}').");
                }
            }
            catch (PlaywrightException ex)
            {
                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] SetValueAsync: cannot click option '{target}' for '{Title}' (Code='{Code}'): {ex.Message}");
                }
                throw;
            }

            await page.WaitForTimeoutAsync(DefaultOptionWaitMs).ConfigureAwait(false);

            var finalValue = await GetValueAsync(debug).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(finalValue))
            {
                throw new InvalidOperationException(
                    $"Failed to set value '{target}' for lookup field '{Title}' (Code='{Code}'): resulting value is empty.");
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] SetValueAsync: final value for '{Title}' (Code='{Code}') = '{finalValue}'.");
            }
        }

        public void SetValue(string optionText, bool debug = false)
        {
            SetValueAsync(optionText, debug).GetAwaiter().GetResult();
        }

        public async Task SelectOptionAsync(string optionText, bool debug = false)
        {
            await SetValueAsync(optionText, debug).ConfigureAwait(false);
        }

        public void SelectOption(string optionText, bool debug = false)
        {
            SelectOptionAsync(optionText, debug).GetAwaiter().GetResult();
        }

        #endregion

        #region Get available options

        /// <summary>
        /// Возвращает список текстов всех доступных (не service и не disabled) опций,
        /// которые видны в текущем dropdown.
        /// </summary>
        public async Task<List<string>> GetAvailableOptionsAsync(bool debug = false)
        {
            if (Page.Page == null)
            {
                throw new InvalidOperationException("Page is not initialized.");
            }

            var page = Page.Page;

            var root = await FindFieldContainerAsync(debug).ConfigureAwait(false);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Field '{Title}' (Code='{Code}') not found on page.");
            }

            var input = GetValueLocator(root);
            await input.ClickAsync().ConfigureAwait(false);

            var panelLocator = page.Locator("div.mat-autocomplete-panel.mat-autocomplete-visible");
            await panelLocator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = DropdownVisibleTimeoutMs
            }).ConfigureAwait(false);

            var dropdown = panelLocator.First;
            var optionsLocator = dropdown.Locator("mat-option");
            var count = await optionsLocator.CountAsync().ConfigureAwait(false);

            var list = new List<string>();

            for (var i = 0; i < count; i++)
            {
                var opt = optionsLocator.Nth(i);

                string? id = null;
                string? ariaDisabled = null;
                try { id = await opt.GetAttributeAsync("id").ConfigureAwait(false); } catch { }
                try { ariaDisabled = await opt.GetAttributeAsync("aria-disabled").ConfigureAwait(false); } catch { }

                var text = await GetOptionTextAsync(opt).ConfigureAwait(false);
                var info = new OptionInfo
                {
                    Index = i,
                    Id = id,
                    AriaDisabled = ariaDisabled,
                    Text = text
                };

                if (IsDisabled(info) || IsServiceOption(info))
                {
                    if (debug)
                    {
                        FieldLogger.Write(
                            $"[Field:{FieldTypeName}] GetAvailableOptionsAsync: skip option #{i}: id='{info.Id}', aria-disabled='{info.AriaDisabled}', text='{SafeLog(info.Text)}'.");
                    }
                    continue;
                }

                if (debug)
                {
                    FieldLogger.Write(
                        $"[Field:{FieldTypeName}] GetAvailableOptionsAsync: option #{i}: id='{info.Id}', text='{SafeLog(info.Text)}'.");
                }

                if (!string.IsNullOrWhiteSpace(info.Text))
                {
                    list.Add(info.Text);
                }
            }

            if (debug)
            {
                FieldLogger.Write(
                    $"[Field:{FieldTypeName}] GetAvailableOptionsAsync: collected {list.Count} options: [{string.Join(", ", list)}].");
            }

            return list;
        }

        public List<string> GetAvailableOptions(bool debug = false)
        {
            return GetAvailableOptionsAsync(debug).GetAwaiter().GetResult();
        }

        #endregion
    }
}
