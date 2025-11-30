using System;

namespace CreatioAutoTestsPlaywright.Tools
{
    /// <summary>
    /// Simple pluggable logger for tests and page/field objects.
    /// By default writes to Console, but can be reassigned in tests.
    /// </summary>
    public static class FieldLogger
    {
        /// <summary>
        /// Delegate used for logging. Can be replaced in test setup.
        /// </summary>
        public static Action<string> Log { get; set; } = Console.WriteLine;

        public static void Write(string message)
        {
            Log?.Invoke(message);
        }
    }
}
