using Microsoft.AspNetCore.Identity;

namespace Docly.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}