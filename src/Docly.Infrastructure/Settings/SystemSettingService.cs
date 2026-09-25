using System.Globalization;
using Docly.Application.Common;
using Docly.Application.Settings;
using Docly.Domain.Settings;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Settings;

public class SystemSettingService : ISystemSettingService
{
    private readonly DoclyDbContext _db;

    public SystemSettingService(DoclyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SystemSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, cancellationToken);

        return SystemSettingCatalog.Settings
            .Select(definition => settings.TryGetValue(definition.Key, out var setting)
                ? new SystemSettingDto(definition.Key, definition.Name, definition.Description, definition.Type, setting.Value)
                : new SystemSettingDto(definition.Key, definition.Name, definition.Description, definition.Type, definition.DefaultValue))
            .ToList();
    }

    public async Task<Result> UpdateAsync(
        IReadOnlyDictionary<string, string> values, Guid updatedByUserId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var validated = new Dictionary<string, string>();

        foreach (var definition in SystemSettingCatalog.Settings)
        {
            if (!values.TryGetValue(definition.Key, out var rawValue))
            {
                continue;
            }

            if (!TryValidate(definition, rawValue, out var normalized, out var error))
            {
                errors.Add($"{definition.Name}: {error}");
                continue;
            }

            validated[definition.Key] = normalized;
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        if (await ExceedsChunkSizeAsync(validated, cancellationToken))
        {
            return Result.Failure("El solapamiento entre fragmentos debe ser menor que el tamaño de fragmento.");
        }

        var existing = await _db.SystemSettings
            .Where(s => validated.Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var (key, value) in validated)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                setting.Value = value;
                setting.UpdatedAtUtc = now;
                setting.UpdatedByUserId = updatedByUserId;
            }
            else
            {
                var definition = SystemSettingCatalog.Settings.First(d => d.Key == key);
                _db.SystemSettings.Add(new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = definition.Key,
                    Name = definition.Name,
                    Description = definition.Description,
                    Type = definition.Type,
                    Value = value,
                    UpdatedAtUtc = now,
                    UpdatedByUserId = updatedByUserId,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<bool> ExceedsChunkSizeAsync(
        Dictionary<string, string> validated, CancellationToken cancellationToken)
    {
        var chunkSize = validated.TryGetValue(SystemSettingKeys.RagChunkSize, out var sizeValue)
            ? int.Parse(sizeValue, CultureInfo.InvariantCulture)
            : await GetCurrentIntAsync(SystemSettingKeys.RagChunkSize, cancellationToken);

        var chunkOverlap = validated.TryGetValue(SystemSettingKeys.RagChunkOverlap, out var overlapValue)
            ? int.Parse(overlapValue, CultureInfo.InvariantCulture)
            : await GetCurrentIntAsync(SystemSettingKeys.RagChunkOverlap, cancellationToken);

        return chunkOverlap >= chunkSize;
    }

    private async Task<int> GetCurrentIntAsync(string key, CancellationToken cancellationToken)
    {
        var value = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        value ??= SystemSettingCatalog.Settings.First(d => d.Key == key).DefaultValue;
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static bool TryValidate(
        SystemSettingDefinition definition, string rawValue, out string normalized, out string? error)
    {
        rawValue = rawValue.Trim();

        switch (definition.Type)
        {
            case SystemSettingType.Integer:
                if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) || intValue <= 0)
                {
                    (normalized, error) = (string.Empty, "debe ser un número entero mayor que cero.");
                    return false;
                }
                (normalized, error) = (intValue.ToString(CultureInfo.InvariantCulture), null);
                return true;

            case SystemSettingType.Decimal:
                if (!decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue)
                    || decimalValue < 0 || decimalValue > 1)
                {
                    (normalized, error) = (string.Empty, "debe ser un número entre 0 y 1.");
                    return false;
                }
                (normalized, error) = (decimalValue.ToString(CultureInfo.InvariantCulture), null);
                return true;

            default:
                if (rawValue.Length == 0)
                {
                    (normalized, error) = (string.Empty, "no puede estar vacío.");
                    return false;
                }
                (normalized, error) = (rawValue, null);
                return true;
        }
    }
}
