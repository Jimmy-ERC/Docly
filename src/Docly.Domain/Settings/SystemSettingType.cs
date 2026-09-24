namespace Docly.Domain.Settings;

/// <summary>How a <see cref="SystemSetting.Value"/> should be parsed and validated.</summary>
public enum SystemSettingType
{
    Text,
    Integer,
    Decimal,
}
