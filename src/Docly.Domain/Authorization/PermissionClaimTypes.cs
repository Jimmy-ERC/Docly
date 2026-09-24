namespace Docly.Domain.Authorization;

/// <summary>Claim types issued at sign-in so authorization checks don't need a database round trip.</summary>
public static class PermissionClaimTypes
{
    public const string Permission = "docly:permission";
    public const string FullName = "docly:full_name";
}
