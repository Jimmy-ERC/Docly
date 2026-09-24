using Docly.Application.Common;

namespace Docly.Application.Authentication;

public sealed record RegisterRequest(string FullName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public interface IAuthService
{
    /// <summary>Creates the account with the default role and signs the new user in.</summary>
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Validates credentials (with lockout) and signs the user in, unless the account is deactivated.</summary>
    Task<Result> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync();
}
