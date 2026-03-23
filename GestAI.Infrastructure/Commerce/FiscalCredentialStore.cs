using GestAI.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace GestAI.Infrastructure.Commerce;

public sealed class FiscalCredentialStore(IWebHostEnvironment environment) : IFiscalCredentialStore
{
    public async Task<string> SaveAsync(int accountId, string fileName, byte[] content, bool isPrivateKey, CancellationToken ct)
    {
        var safeFileName = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = isPrivateKey ? "private.key" : "certificate.crt";

        var extension = Path.GetExtension(safeFileName);
        var prefix = isPrivateKey ? "key" : "cert";
        var stampedFileName = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";

        var root = Path.Combine(environment.ContentRootPath, "App_Data", "fiscal", $"account-{accountId}");
        Directory.CreateDirectory(root);

        var fullPath = Path.Combine(root, stampedFileName);
        await File.WriteAllBytesAsync(fullPath, content, ct);

        return Path.Combine($"account-{accountId}", stampedFileName).Replace('\\', '/');
    }
}
