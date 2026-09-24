using Docly.Application.Authentication;
using Docly.Application.Common;
using Docly.Domain.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Docly.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Correo o contraseña incorrectos.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result.Failure(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Default);
        if (!roleResult.Succeeded)
        {
            // Don't leave an account without a role behind (e.g. if the seeder hasn't run).
            await _userManager.DeleteAsync(user);
            return Result.Failure("No se pudo completar el registro. Inténtalo de nuevo más tarde.");
        }

        // Sign in after the role exists so the permission claims are included in the cookie.
        await _signInManager.SignInAsync(user, isPersistent: false);
        return Result.Success();
    }

    public async Task<Result> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Result.Failure(InvalidCredentials);
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (check.IsLockedOut)
        {
            return Result.Failure("Demasiados intentos fallidos. Inténtalo de nuevo en unos minutos.");
        }

        if (!check.Succeeded)
        {
            return Result.Failure(InvalidCredentials);
        }

        // Only reveal that the account is deactivated once the password is proven correct.
        if (!user.IsActive)
        {
            return Result.Failure("Tu cuenta está desactivada. Contacta con un administrador.");
        }

        await _signInManager.SignInAsync(user, request.RememberMe);
        return Result.Success();
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();
}
