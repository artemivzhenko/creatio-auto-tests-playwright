using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CreatioAutoTestsPlaywright.Config
{
    /// <summary>
    /// Root configuration object that describes all UI pages under test.
    /// </summary>
    public sealed class UiTestConfig
    {
        /// <summary>
        /// Collection of page configurations.
        /// </summary>
        [JsonPropertyName("pages")]
        public List<PageConfig> Pages { get; set; } = new List<PageConfig>();
    }

    /// <summary>
    /// Configuration of a single Creatio page under test.
    /// </summary>
    public sealed class PageConfig
    {
        /// <summary>
        /// Logical name of the page (used in tests to select the page).
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Absolute or relative URL of the page (Creatio shell URL).
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Page type, for example "FreedomUI".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Field definitions for this page.
        /// </summary>
        [JsonPropertyName("fields")]
        public List<FieldConfig> Fields { get; set; } = new List<FieldConfig>();

        /// <summary>
        /// Button definitions for this page.
        /// </summary>
        [JsonPropertyName("buttons")]
        public List<ButtonConfig> Buttons { get; set; } = new List<ButtonConfig>();
    }

    /// <summary>
    /// Configuration of a single field on a page.
    /// </summary>
    public sealed class FieldConfig
    {
        /// <summary>
        /// High-level field type, e.g. "Text", "Number", "DateTime", "Boolean", "Lookup".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Field subtype, e.g. "Text", "RichText", "Link", "Integer", "Decimal", "DateTime".
        /// </summary>
        [JsonPropertyName("subtype")]
        public string Subtype { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly title shown on UI (label).
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Field code, usually equal to Freedom UI element-name / id.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Indicates that this field is required according to business rules.
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        /// <summary>
        /// Indicates that this field should be read-only on UI.
        /// </summary>
        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Expected placeholder text. Can be empty or null when not used.
        /// </summary>
        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }
    }

    /// <summary>
    /// Configuration of a single button on a page.
    /// </summary>
    public sealed class ButtonConfig
    {
        /// <summary>
        /// Human-friendly button caption. May be empty when the button is identified only by code.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Button code (element-name / id of crt-button).
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }
}
