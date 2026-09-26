using Docly.Domain.Settings;

namespace Docly.Web.ViewModels.Settings;

public class SystemSettingItemViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SystemSettingType Type { get; set; }
    public string Value { get; set; } = string.Empty;

    /// <summary>Display-only bounds for the input; the server re-validates on save.</summary>
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    public bool IsRag => Key.StartsWith(SystemSettingCatalog.RagKeyPrefix, StringComparison.Ordinal);
}

public class SystemSettingsViewModel
{
    public List<SystemSettingItemViewModel> Settings { get; set; } = [];
}
