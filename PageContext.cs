using System;
using System.Collections.Generic;
using CreatioAutoTestsPlaywright.Config;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Runtime context for a Creatio page under test.
    /// Holds the CreatioPage instance, JSON configuration and resolved fields/buttons.
    /// </summary>
    public sealed class PageContext
    {
        /// <summary>
        /// Underlying CreatioPage used to interact with the browser.
        /// </summary>
        public CreatioPage Page { get; }

        /// <summary>
        /// Configuration of this page loaded from JSON.
        /// </summary>
        public PageConfig Config { get; }

        /// <summary>
        /// Fields on the page, indexed by field code.
        /// </summary>
        public IReadOnlyDictionary<string, IField> Fields { get; }

        /// <summary>
        /// Buttons on the page, indexed by button code.
        /// </summary>
        public IReadOnlyDictionary<string, IButton> Buttons { get; }

        /// <summary>
        /// Creates a new PageContext instance.
        /// </summary>
        /// <param name="page">CreatioPage instance.</param>
        /// <param name="config">Page configuration loaded from JSON.</param>
        /// <param name="fields">Dictionary of fields indexed by field code.</param>
        /// <param name="buttons">Dictionary of buttons indexed by button code.</param>
        public PageContext(
            CreatioPage page,
            PageConfig config,
            Dictionary<string, IField> fields,
            Dictionary<string, IButton> buttons)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Buttons = buttons ?? throw new ArgumentNullException(nameof(buttons));
        }

        /// <summary>
        /// Returns a field by its code and casts it to the requested type.
        /// Throws if the field is not found or cannot be cast to TField.
        /// </summary>
        /// <typeparam name="TField">Expected field type implementing IField.</typeparam>
        /// <param name="code">Field code as defined in JSON.</param>
        /// <returns>Field instance of type TField.</returns>
        public TField GetField<TField>(string code) where TField : class, IField
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Field code must be provided.", nameof(code));
            }

            if (!Fields.TryGetValue(code, out var field))
            {
                throw new KeyNotFoundException(
                    $"Field with code '{code}' was not found in PageContext for page '{Config.Name}'.");
            }

            if (field is TField typedField)
            {
                return typedField;
            }

            throw new InvalidCastException(
                $"Field with code '{code}' is of type '{field.GetType().Name}', " +
                $"which cannot be cast to '{typeof(TField).Name}'.");
        }

        /// <summary>
        /// Returns a field by its code as IField.
        /// Throws if the field is not found.
        /// </summary>
        /// <param name="code">Field code as defined in JSON.</param>
        /// <returns>Field instance implementing IField.</returns>
        public IField GetField(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Field code must be provided.", nameof(code));
            }

            if (!Fields.TryGetValue(code, out var field))
            {
                throw new KeyNotFoundException(
                    $"Field with code '{code}' was not found in PageContext for page '{Config.Name}'.");
            }

            return field;
        }

        /// <summary>
        /// Returns a button by its code.
        /// Throws if the button is not found.
        /// </summary>
        /// <param name="code">Button code as defined in JSON.</param>
        /// <returns>Button instance implementing IButton.</returns>
        public IButton GetButton(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Button code must be provided.", nameof(code));
            }

            if (!Buttons.TryGetValue(code, out var button))
            {
                throw new KeyNotFoundException(
                    $"Button with code '{code}' was not found in PageContext for page '{Config.Name}'.");
            }

            return button;
        }

        /// <summary>
        /// Tries to get a field by its code. Returns null if not found or cannot be cast to TField.
        /// </summary>
        /// <typeparam name="TField">Expected field type implementing IField.</typeparam>
        /// <param name="code">Field code as defined in JSON.</param>
        /// <returns>Field instance of type TField or null.</returns>
        public TField? TryGetField<TField>(string code) where TField : class, IField
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            if (!Fields.TryGetValue(code, out var field))
            {
                return null;
            }

            return field as TField;
        }

        /// <summary>
        /// Tries to get a button by its code. Returns null if not found.
        /// </summary>
        /// <param name="code">Button code as defined in JSON.</param>
        /// <returns>Button instance implementing IButton or null.</returns>
        public IButton? TryGetButton(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            if (!Buttons.TryGetValue(code, out var button))
            {
                return null;
            }

            return button;
        }
    }
}
