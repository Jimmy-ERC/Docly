using Docly.Application.Common;
using Docly.Domain.Settings;

namespace Docly.Application.Settings;

public sealed record SystemSettingDto(
    string Key, string Name, string? Description, SystemSettingType Type, string Value,
    decimal? MinValue = null, decimal? MaxValue = null);

public interface ISystemSettingService
{
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates every provided key at once, validating each value against its type before saving any of them.</summary>
    Task<Result> UpdateAsync(
        IReadOnlyDictionary<string, string> values, Guid updatedByUserId, CancellationToken cancellationToken = default);
}
