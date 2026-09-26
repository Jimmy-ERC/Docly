namespace Docly.Domain.Settings;

/// <param name="MinValue">Inclusive lower bound for numeric settings; defaults to 1 for integers and 0 for decimals.</param>
/// <param name="MaxValue">Inclusive upper bound for numeric settings; defaults to none for integers and 1 for decimals.</param>
public sealed record SystemSettingDefinition(
    string Key, string Name, string? Description, SystemSettingType Type, string DefaultValue,
    decimal? MinValue = null, decimal? MaxValue = null);

/// <summary>Source of truth for the seeded settings. The embedding model and its dimensionality are
/// deliberately not configurable here: changing them after chunks were embedded would make existing
/// vectors incomparable to new ones (see Docly's project notes on Gemini embeddings).</summary>
public static class SystemSettingCatalog
{
    /// <summary>Settings whose key starts with this prefix belong to the RAG pipeline.</summary>
    public const string RagKeyPrefix = "rag.";

    public static readonly IReadOnlyList<SystemSettingDefinition> Settings =
    [
        // The embedding model accepts up to 2048 tokens per input; 6000 characters stays safely below that.
        new(SystemSettingKeys.RagChunkSize, "Tamaño de fragmento",
            "Cantidad de caracteres por fragmento al dividir un documento (100 a 6000).",
            SystemSettingType.Integer, "1000", MinValue: 100, MaxValue: 6000),

        new(SystemSettingKeys.RagChunkOverlap, "Solapamiento entre fragmentos",
            "Caracteres que se repiten entre fragmentos consecutivos. Debe ser menor que el tamaño de fragmento.",
            SystemSettingType.Integer, "200", MinValue: 0, MaxValue: 2000),

        new(SystemSettingKeys.RagTopK, "Fragmentos recuperados (top-K)",
            "Cantidad de fragmentos que la búsqueda híbrida recupera antes de generar la respuesta (1 a 50).",
            SystemSettingType.Integer, "5", MinValue: 1, MaxValue: 50),

        new(SystemSettingKeys.RagSimilarityThreshold, "Umbral de similitud",
            "Similitud coseno mínima (entre 0 y 1) para considerar relevante un fragmento.",
            SystemSettingType.Decimal, "0.70"),

        new(SystemSettingKeys.RagEmbeddingBatchSize, "Fragmentos por lote de embeddings",
            "Cantidad de fragmentos enviados a Gemini por solicitud al indexar un documento (1 a 100).",
            SystemSettingType.Integer, "50", MinValue: 1, MaxValue: 100),

        new(SystemSettingKeys.DocumentsMaxUploadSizeMb, "Tamaño máximo de archivo (MB)",
            "Límite de tamaño por archivo validado por la aplicación, además del límite del bucket.",
            SystemSettingType.Integer, "20"),
    ];
}
