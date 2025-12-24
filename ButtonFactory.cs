using System;
using CreatioAutoTestsPlaywright.Config;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Factory responsible for creating button instances from JSON configuration.
    /// </summary>
    public static class ButtonFactory
    {
        /// <summary>
        /// Creates a concrete button instance based on the given configuration.
        /// </summary>
        /// <param name="page">Creatio page object used by the button.</param>
        /// <param name="cfg">Button configuration loaded from JSON.</param>
        /// <returns>Concrete implementation of IButton.</returns>
        public static Button CreateButton(CreatioPage page, ButtonConfig cfg)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            if (string.IsNullOrEmpty(cfg.Code))
            {
                throw new ArgumentException("ButtonConfig.Code must not be empty.", nameof(cfg));
            }

            // Title may be empty, this means the button is matched only by Code.
            var title = cfg.Title ?? string.Empty;

            return new Button(page, title, cfg.Code);
        }
    }
}