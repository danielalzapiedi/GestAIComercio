using GestAI.Application.Abstractions;
using GestAI.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GestAI.Infrastructure.Identity;

public class IdentityService(UserManager<User> userManager) : IIdentityService
{
    public async Task<(bool Success, string? UserId, string? Error)> FindUserIdByEmailAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        return (true, user?.Id, null);
    }

    public async Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct)
        => await CreateUserIfNotExistsAsync(email, password, ct, string.Empty, string.Empty, true, null, 0);

    public async Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct, string firstName, string lastName, bool isActive, int? defaultPropertyId, int defaultAccountId)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null) return (true, user.Id, null);

        user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = string.IsNullOrWhiteSpace(firstName) ? email : firstName,
            Apellido = string.IsNullOrWhiteSpace(lastName) ? "Usuario" : lastName,
            IsActive = isActive,
            DefaultAccountId = defaultAccountId
        };
        var res = await userManager.CreateAsync(user, password);

        if (!res.Succeeded)
            return (false, null, string.Join(" | ", res.Errors.Select(e => e.Description)));

        return (true, user.Id, null);
    }
}
