namespace Docly.Domain.Settings;

/// <summary>A single configurable system value (e.g. RAG chunk size). See <see cref="SystemSettingKeys"/>.</summary>
public class SystemSetting
{
    public Guid Id { get; set; }

    /// <summary>Stable machine-readable key, unique. See <see cref="SystemSettingKeys"/>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Stored as text; parsed according to <see cref="Type"/> when consumed.</summary>
    public string Value { get; set; } = string.Empty;

    public SystemSettingType Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
