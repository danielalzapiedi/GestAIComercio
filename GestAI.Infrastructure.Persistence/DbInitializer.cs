using GestAI.Domain.Entities;
using GestAI.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestAI.Infrastructure.Persistence;

public static class DbInitializer
{
    public sealed record SeedOptions(string AdminEmail, string AdminPassword, string PropertyName, string[] UnitNames);

    public static async Task MigrateAndSeedAsync(AppDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger, SeedOptions options, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (!await roleManager.RoleExistsAsync("Admin")) await roleManager.CreateAsync(new IdentityRole("Admin"));

        var admin = await userManager.FindByEmailAsync(options.AdminEmail);
        if (admin is null)
        {
            admin = new User { UserName = options.AdminEmail, Email = options.AdminEmail, EmailConfirmed = true, Nombre = "Admin", Apellido = "GestAI" };
            var created = await userManager.CreateAsync(admin, options.AdminPassword);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.OwnerUserId == admin.Id, ct);
        if (account is null)
        {
            account = new Account { OwnerUserId = admin.Id, Name = "Mi cuenta", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            db.Accounts.Add(account);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.SaasPlanDefinitions.AnyAsync(ct))
        {
            db.SaasPlanDefinitions.AddRange(
                new SaasPlanDefinition { Code = SaasPlanCode.Starter, Name = "Starter", MaxProperties = 1, MaxUnits = 5, MaxUsers = 2, IncludesOperations = false, IncludesPublicPortal = false, IncludesReports = false },
                new SaasPlanDefinition { Code = SaasPlanCode.Pro, Name = "Pro", MaxProperties = 3, MaxUnits = 20, MaxUsers = 5, IncludesOperations = true, IncludesPublicPortal = true, IncludesReports = true },
                new SaasPlanDefinition { Code = SaasPlanCode.Manager, Name = "Manager", MaxProperties = 10, MaxUnits = 100, MaxUsers = 20, IncludesOperations = true, IncludesPublicPortal = true, IncludesReports = true });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AccountSubscriptionPlans.AnyAsync(x => x.AccountId == account.Id && x.IsActive, ct))
        {
            var proPlan = await db.SaasPlanDefinitions.FirstAsync(x => x.Code == SaasPlanCode.Pro, ct);
            db.AccountSubscriptionPlans.Add(new AccountSubscriptionPlan { AccountId = account.Id, PlanDefinitionId = proPlan.Id, IsActive = true });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AccountUsers.AnyAsync(x => x.AccountId == account.Id && x.UserId == admin.Id, ct))
        {
            db.AccountUsers.Add(new AccountUser
            {
                AccountId = account.Id,
                UserId = admin.Id,
                Role = InternalUserRole.Owner,
                IsActive = true,
                CanManageConfiguration = true,
                InvitedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        var adminTracked = await db.Users.FirstAsync(u => u.Id == admin.Id, ct);
        if (adminTracked.DefaultAccountId == 0)
        {
            adminTracked.DefaultAccountId = account.Id;
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("DB migrate + seed OK. Admin: {Email}, AccountId: {AccountId}", options.AdminEmail, account.Id);
    }
}
