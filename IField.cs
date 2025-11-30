using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Common contract for all field types on Creatio Freedom UI pages.
    /// </summary>
    public interface IField
    {
        string Title { get; }
        string Code { get; }

        bool ReadOnly { get; }
        bool Required { get; }
        string? Placeholder { get; }

        Task<bool> CheckIfExistAsync(bool debug = false, int? timeoutOverrideMs = null);
        bool CheckIfExist(bool debug = false, int? timeoutOverrideMs = null);

        Task<bool> CheckIfReadOnlyAsync(bool debug = false);
        bool CheckIfReadOnly(bool debug = false);

        Task<bool> CheckIfRequiredAsync(bool debug = false);
        bool CheckIfRequired(bool debug = false);

        Task<bool> CheckPlaceholderAsync(bool debug = false);
        bool CheckPlaceholder(bool debug = false);

        Task<bool> CheckFieldAsync(bool debug = false, int? timeoutOverrideMs = null);
        bool CheckField(bool debug = false, int? timeoutOverrideMs = null);
    }
}