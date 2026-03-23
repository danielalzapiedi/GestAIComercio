using System.Text.Json;
using GestAI.Application.Abstractions;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;

namespace GestAI.Infrastructure.Commerce;

public sealed class FiscalIntegrationService : IFiscalIntegrationService
{
    public Task<FiscalAuthorizationResult> AuthorizeInvoiceAsync(CommercialInvoice invoice, FiscalConfiguration configuration, CancellationToken ct)
    {
        var requestPayload = JsonSerializer.Serialize(new
        {
            invoice.Number,
            invoice.InvoiceType,
            invoice.PointOfSale,
            invoice.SequentialNumber,
            invoice.IssuedAtUtc,
            invoice.CustomerId,
            invoice.Total,
            configuration.LegalName,
            configuration.TaxIdentifier,
            configuration.IntegrationMode,
            configuration.UseSandbox
        });

        if (configuration.IntegrationMode == FiscalIntegrationMode.Mock)
        {
            var cae = $"MOCK{invoice.PointOfSale:D4}{invoice.SequentialNumber:D8}";
            var responsePayload = JsonSerializer.Serialize(new
            {
                result = "authorized",
                cae,
                dueDate = DateTime.UtcNow.Date.AddDays(10),
                provider = configuration.UseSandbox ? "arca-sandbox-mock" : "arca-mock"
            });

            return Task.FromResult(new FiscalAuthorizationResult(
                FiscalSubmissionStatus.Authorized,
                requestPayload,
                responsePayload,
                null,
                cae,
                DateTime.UtcNow.Date.AddDays(10),
                cae,
                configuration.UseSandbox ? "Autorizada por sandbox mock." : "Autorizada por adaptador mock."));
        }

        var errorPayload = JsonSerializer.Serialize(new
        {
            result = "error",
            message = "El adaptador real de ARCA quedó preparado pero requiere credenciales/certificados y endpoint productivo para operar."
        });

        return Task.FromResult(new FiscalAuthorizationResult(
            FiscalSubmissionStatus.Error,
            requestPayload,
            errorPayload,
            "Integración real ARCA no configurada en este entorno.",
            null,
            null,
            null,
            "Error de integración ARCA: faltan credenciales o endpoint real."));
    }
}
