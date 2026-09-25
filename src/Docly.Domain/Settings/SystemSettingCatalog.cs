namespace Docly.Domain.Settings;

public sealed record SystemSettingDefinition(
    string Key, string Name, string? Description, SystemSettingType Type, string DefaultValue);

/// <summary>Source of truth for the seeded settings. The embedding model and its dimensionality are
/// deliberately not configurable here: changing them after chunks were embedded would make existing
/// vectors incomparable to new ones (see Docly's project notes on Gemini embeddings).</summary>
public static class SystemSettingCatalog
{
    public static readonly IReadOnlyList<SystemSettingDefinition> Settings =
    [
        new(SystemSettingKeys.RagChunkSize, "Tamaño de fragmento",
            "Cantidad de caracteres por fragmento al dividir un documento.", SystemSettingType.Integer, "1000"),

        new(SystemSettingKeys.RagChunkOverlap, "Solapamiento entre fragmentos",
            "Caracteres que se repiten entre fragmentos consecutivos.", SystemSettingType.Integer, "200"),

        new(SystemSettingKeys.RagTopK, "Fragmentos recuperados (top-K)",
            "Cantidad de fragmentos que la búsqueda híbrida recupera antes de generar la respuesta.",
            SystemSettingType.Integer, "5"),

        new(SystemSettingKeys.RagSimilarityThreshold, "Umbral de similitud",
            "Similitud coseno mínima (entre 0 y 1) para considerar relevante un fragmento.",
            SystemSettingType.Decimal, "0.70"),

        new(SystemSettingKeys.DocumentsMaxUploadSizeMb, "Tamaño máximo de archivo (MB)",
            "Límite de tamaño por archivo validado por la aplicación, además del límite del bucket.",
            SystemSettingType.Integer, "20"),
    ];
}
