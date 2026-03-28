using AutoMapper;
using FluentValidation;
using GestAI.Api.Middleware;
using GestAI.Application;
using GestAI.Application.Abstractions;
using GestAI.Application.Behaviors;
using GestAI.Application.Mapping;
using GestAI.Application.Security;
using GestAI.Domain.Entities;
using GestAI.Infrastructure;
using GestAI.Infrastructure.Identity;
using GestAI.Infrastructure.Commerce;
using GestAI.Infrastructure.Payments;
using GestAI.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using System.Security.Cryptography;
using System.Text.Json;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("all", p =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var normalizedOrigins = allowedOrigins
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        p.AllowAnyHeader().AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
        {
            if (normalizedOrigins.Length == 0)
            {
                p.AllowAnyOrigin();
                return;
            }

            p.WithOrigins(normalizedOrigins);
            return;
        }

        if (normalizedOrigins.Length == 0)
            throw new InvalidOperationException("Configurá Cors:AllowedOrigins para ambientes no Development.");

        p.WithOrigins(normalizedOrigins);
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GestAI.Application.AssemblyMarker).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(GestAI.Application.AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(MappingProfile).Assembly);
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AppResultHttpMappingFilter>();
}).AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPayPal(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

// Interfaces Application
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IUserAccessService, GestAI.Infrastructure.Saas.UserAccessService>();
builder.Services.AddScoped<ISaasPlanService, GestAI.Infrastructure.Saas.SaasPlanService>();
builder.Services.AddScoped<IAuditService, GestAI.Infrastructure.Saas.AuditService>();
builder.Services.AddScoped<IFiscalIntegrationService, FiscalIntegrationService>();
builder.Services.AddScoped<IFiscalCredentialStore, FiscalCredentialStore>();
builder.Services.AddScoped<ICommercialDocumentPdfService, CommercialDocumentPdfService>();

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;

app.UseSwagger();
app.UseSwaggerUI();

app.UseResponseCompression();
app.UseCors("all");
app.UseAuthentication();
app.UseAuthorization();

app.UseApiExceptionHandling();

app.MapControllers();

// migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    var seedAdminPassword = builder.Configuration["Seed:AdminPassword"];
    var seedDemoOwnerPassword = builder.Configuration["Seed:DemoOwnerPassword"];
    var isDevelopment = app.Environment.IsDevelopment();
    var logGeneratedSeedPasswords = builder.Configuration.GetValue<bool>("Seed:LogGeneratedPasswords");

    if (string.IsNullOrWhiteSpace(seedAdminPassword) || string.IsNullOrWhiteSpace(seedDemoOwnerPassword))
    {
        if (!isDevelopment)
        {
            throw new InvalidOperationException("Faltan credenciales de seed. Configurá Seed:AdminPassword y Seed:DemoOwnerPassword.");
        }

        if (string.IsNullOrWhiteSpace(seedAdminPassword))
        {
            seedAdminPassword = GenerateSeedPassword();
            if (logGeneratedSeedPasswords)
            {
                logger.LogWarning("Seed:AdminPassword no está configurado en Development. Contraseña temporal generada para admin: {Password}", seedAdminPassword);
            }
            else
            {
                logger.LogWarning("Seed:AdminPassword no está configurado en Development. Se generó una contraseña temporal (oculta). Para mostrarla en logs, activá Seed:LogGeneratedPasswords=true.");
            }
        }

        if (string.IsNullOrWhiteSpace(seedDemoOwnerPassword))
        {
            seedDemoOwnerPassword = GenerateSeedPassword();
            if (logGeneratedSeedPasswords)
            {
                logger.LogWarning("Seed:DemoOwnerPassword no está configurado en Development. Contraseña temporal generada para demo owner: {Password}", seedDemoOwnerPassword);
            }
            else
            {
                logger.LogWarning("Seed:DemoOwnerPassword no está configurado en Development. Se generó una contraseña temporal (oculta). Para mostrarla en logs, activá Seed:LogGeneratedPasswords=true.");
            }
        }
    }

    if (!isDevelopment && logGeneratedSeedPasswords)
    {
        throw new InvalidOperationException("Seed:LogGeneratedPasswords=true no está permitido fuera de Development.");
    }

    ValidateSeedPasswordPolicy(seedAdminPassword, nameof(seedAdminPassword), isDevelopment);
    ValidateSeedPasswordPolicy(seedDemoOwnerPassword, nameof(seedDemoOwnerPassword), isDevelopment);

    logger.LogInformation("Inicializando datos seed con credenciales configurables por entorno (Seed:*).");

    await DbInitializer.MigrateAndSeedAsync(
        db,
        userMgr,
        roleMgr,
        logger,
        new DbInitializer.SeedOptions(
            AdminEmail: "admin@local.test",
            AdminPassword: seedAdminPassword,
            PropertyName: "Tenant Demo",
            UnitNames: new[] { "Workspace A", "Workspace B" },
            DemoOwnerEmail: "daniel@daniel.com",
            DemoOwnerPassword: seedDemoOwnerPassword
        )
    );
}

app.Run();

static string GenerateSeedPassword()
{
    const string lower = "abcdefghijkmnopqrstuvwxyz";
    const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string digits = "23456789";
    const string symbols = "!@$%*?";
    var all = lower + upper + digits + symbols;

    Span<char> chars = stackalloc char[14];
    chars[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
    chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
    chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
    chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

    for (var i = 4; i < chars.Length; i++)
    {
        chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
    }

    for (var i = chars.Length - 1; i > 0; i--)
    {
        var j = RandomNumberGenerator.GetInt32(i + 1);
        (chars[i], chars[j]) = (chars[j], chars[i]);
    }

    return new string(chars);
}

static void ValidateSeedPasswordPolicy(string? password, string name, bool isDevelopment)
{
    if (string.IsNullOrWhiteSpace(password))
        return;

    if (isDevelopment)
        return;

    var hasLower = password.Any(char.IsLower);
    var hasUpper = password.Any(char.IsUpper);
    var hasDigit = password.Any(char.IsDigit);
    var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

    if (password.Length < 12 || !hasLower || !hasUpper || !hasDigit || !hasSymbol)
    {
        throw new InvalidOperationException($"La credencial {name} no cumple la política mínima para ambientes no Development.");
    }
}
