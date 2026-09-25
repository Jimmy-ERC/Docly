namespace Docly.Infrastructure.Storage;

/// <summary>Bound from the "Supabase" configuration section (user-secrets).</summary>
public class SupabaseStorageOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "documents";
}
