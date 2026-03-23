using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using GestAI.Application.Abstractions;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GestAI.Infrastructure.Commerce;

public sealed class FiscalIntegrationService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IWebHostEnvironment environment,
    ILogger<FiscalIntegrationService> logger) : IFiscalIntegrationService
{
    private const string WsaaServiceName = "wsfe";
    private const string WsaaHomoUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
    private const string WsaaProdUrl = "https://wsaa.afip.gov.ar/ws/services/LoginCms";
    private const string WsfeHomoUrl = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
    private const string WsfeProdUrl = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";
    private const string SoapEnvelopeNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WsaaNs = "http://wsaa.view.sua.dvadac.desein.afip.gov";
    private const string WsfeNs = "http://ar.gov.afip.dif.FEV1/";

    public async Task<FiscalAuthorizationResult> AuthorizeInvoiceAsync(CommercialInvoice invoice, FiscalConfiguration configuration, CancellationToken ct)
    {
        if (configuration.IntegrationMode == FiscalIntegrationMode.Mock)
            return BuildMockResult(invoice, configuration);

        try
        {
            var auth = await GetAccessTicketAsync(configuration, ct);
            var requestXml = BuildFecaeSolicitarEnvelope(invoice, configuration, auth);
            var responseXml = await PostSoapAsync(GetWsfeUrl(configuration), requestXml, soapAction: $"{WsfeNs}FECAESolicitar", ct);
            return ParseWsfeResponse(invoice, configuration, requestXml, responseXml);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando factura {InvoiceNumber} a ARCA/AFIP.", invoice.Number);

            var errorPayload = JsonSerializer.Serialize(new
            {
                invoice.Number,
                configuration.IntegrationMode,
                configuration.UseSandbox,
                exception = ex.Message
            });

            return new FiscalAuthorizationResult(
                FiscalSubmissionStatus.Error,
                JsonSerializer.Serialize(new { invoice.Number, configuration.PointOfSale, configuration.TaxIdentifier }),
                errorPayload,
                ex.Message,
                null,
                null,
                null,
                $"Error de integración ARCA: {ex.Message}");
        }
    }

    private FiscalAuthorizationResult BuildMockResult(CommercialInvoice invoice, FiscalConfiguration configuration)
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

        var cae = $"MOCK{invoice.PointOfSale:D4}{invoice.SequentialNumber:D8}";
        var responsePayload = JsonSerializer.Serialize(new
        {
            result = "authorized",
            cae,
            dueDate = DateTime.UtcNow.Date.AddDays(10),
            provider = configuration.UseSandbox ? "arca-sandbox-mock" : "arca-mock"
        });

        return new FiscalAuthorizationResult(
            FiscalSubmissionStatus.Authorized,
            requestPayload,
            responsePayload,
            null,
            cae,
            DateTime.UtcNow.Date.AddDays(10),
            cae,
            configuration.UseSandbox ? "Autorizada por sandbox mock." : "Autorizada por adaptador mock.");
    }

    private async Task<AccessTicket> GetAccessTicketAsync(FiscalConfiguration configuration, CancellationToken ct)
    {
        var cacheKey = $"arca-ta:{configuration.UseSandbox}:{configuration.TaxIdentifier}:{configuration.CertificateReference}:{configuration.PrivateKeyReference}";
        if (cache.TryGetValue<AccessTicket>(cacheKey, out var cached) && cached is not null && cached.ExpirationTimeUtc > DateTime.UtcNow.AddMinutes(5))
            return cached;

        var cms = BuildCms(configuration);
        var wsaaEnvelope = BuildWsaaEnvelope(cms);
        var wsaaResponse = await PostSoapAsync(GetWsaaUrl(configuration), wsaaEnvelope, soapAction: string.Empty, ct);
        var accessTicket = ParseWsaaResponse(wsaaResponse);

        cache.Set(cacheKey, accessTicket, accessTicket.ExpirationTimeUtc);
        return accessTicket;
    }

    private string BuildCms(FiscalConfiguration configuration)
    {
        var certificate = LoadCertificate(configuration);
        var traXml = BuildLoginTicketRequest();
        var content = Encoding.UTF8.GetBytes(traXml);
        var signedCms = new SignedCms(new ContentInfo(content), detached: false);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
            IncludeOption = X509IncludeOption.EndCertOnly,
            DigestAlgorithm = new System.Security.Cryptography.Oid("2.16.840.1.101.3.4.2.1")
        };

        signedCms.ComputeSignature(signer);
        return Convert.ToBase64String(signedCms.Encode());
    }

    private X509Certificate2 LoadCertificate(FiscalConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.CertificateReference))
            throw new InvalidOperationException("Falta la referencia del certificado fiscal.");

        var certPath = ResolveStoredPath(configuration.CertificateReference);
        if (!File.Exists(certPath))
            throw new FileNotFoundException("No se encontró el certificado fiscal cargado para la cuenta.", certPath);

        var extension = Path.GetExtension(certPath).ToLowerInvariant();
        if (extension is ".pfx" or ".p12")
            return new X509Certificate2(certPath, string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

        if (string.IsNullOrWhiteSpace(configuration.PrivateKeyReference))
            throw new InvalidOperationException("Falta la clave privada para firmar el LoginTicketRequest.");

        var keyPath = ResolveStoredPath(configuration.PrivateKeyReference);
        if (!File.Exists(keyPath))
            throw new FileNotFoundException("No se encontró la clave privada fiscal cargada para la cuenta.", keyPath);

        var pem = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        return new X509Certificate2(pem.Export(X509ContentType.Pfx), string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }

    private string ResolveStoredPath(string reference)
    {
        var normalized = (reference ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(environment.ContentRootPath, "App_Data", "fiscal", normalized);
    }

    private static string BuildLoginTicketRequest()
    {
        var now = DateTime.UtcNow;
        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("loginTicketRequest",
                new XAttribute("version", "1.0"),
                new XElement("header",
                    new XElement("uniqueId", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    new XElement("generationTime", now.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)),
                    new XElement("expirationTime", now.AddHours(12).ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture))),
                new XElement("service", WsaaServiceName)));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static string BuildWsaaEnvelope(string cms)
    {
        var soapNs = XNamespace.Get(SoapEnvelopeNs);
        var wsaaNs = XNamespace.Get(WsaaNs);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", soapNs),
                new XAttribute(XNamespace.Xmlns + "wsaa", wsaaNs),
                new XElement(soapNs + "Header"),
                new XElement(soapNs + "Body",
                    new XElement(wsaaNs + "loginCms",
                        new XElement(wsaaNs + "in0", cms)))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private string BuildFecaeSolicitarEnvelope(CommercialInvoice invoice, FiscalConfiguration configuration, AccessTicket auth)
    {
        var soapNs = XNamespace.Get(SoapEnvelopeNs);
        var wsfeNs = XNamespace.Get(WsfeNs);
        var docType = ResolveDocumentType(invoice.Customer.DocumentNumber);
        var docNumber = ResolveDocumentNumber(invoice.Customer.DocumentNumber, docType);
        var cbteTipo = ResolveCbteTipo(invoice.InvoiceType);
        var ivaId = ResolveVatRateId(invoice.Items.FirstOrDefault()?.TaxRate ?? 0.21m);
        var invoiceDate = invoice.IssuedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var detail = new XElement(wsfeNs + "FECAEDetRequest",
            new XElement(wsfeNs + "Concepto", 1),
            new XElement(wsfeNs + "DocTipo", docType),
            new XElement(wsfeNs + "DocNro", docNumber),
            new XElement(wsfeNs + "CbteDesde", invoice.SequentialNumber),
            new XElement(wsfeNs + "CbteHasta", invoice.SequentialNumber),
            new XElement(wsfeNs + "CbteFch", invoiceDate),
            new XElement(wsfeNs + "ImpTotal", FormatDecimal(invoice.Total)),
            new XElement(wsfeNs + "ImpTotConc", FormatDecimal(0m)),
            new XElement(wsfeNs + "ImpNeto", FormatDecimal(invoice.Subtotal)),
            new XElement(wsfeNs + "ImpOpEx", FormatDecimal(0m)),
            new XElement(wsfeNs + "ImpTrib", FormatDecimal(invoice.OtherTaxesAmount)),
            new XElement(wsfeNs + "ImpIVA", FormatDecimal(invoice.TaxAmount)),
            new XElement(wsfeNs + "MonId", ResolveCurrencyCode(invoice.CurrencyCode)),
            new XElement(wsfeNs + "MonCotiz", FormatDecimal(1m)));

        if (invoice.TaxAmount > 0m)
        {
            detail.Add(
                new XElement(wsfeNs + "Iva",
                    new XElement(wsfeNs + "AlicIva",
                        new XElement(wsfeNs + "Id", ivaId),
                        new XElement(wsfeNs + "BaseImp", FormatDecimal(invoice.Subtotal)),
                        new XElement(wsfeNs + "Importe", FormatDecimal(invoice.TaxAmount)))));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", soapNs),
                new XAttribute(XNamespace.Xmlns + "ar", wsfeNs),
                new XElement(soapNs + "Header"),
                new XElement(soapNs + "Body",
                    new XElement(wsfeNs + "FECAESolicitar",
                        new XElement(wsfeNs + "Auth",
                            new XElement(wsfeNs + "Token", auth.Token),
                            new XElement(wsfeNs + "Sign", auth.Sign),
                            new XElement(wsfeNs + "Cuit", NormalizeCuit(configuration.TaxIdentifier))),
                        new XElement(wsfeNs + "FeCAEReq",
                            new XElement(wsfeNs + "FeCabReq",
                                new XElement(wsfeNs + "CantReg", 1),
                                new XElement(wsfeNs + "PtoVta", invoice.PointOfSale),
                                new XElement(wsfeNs + "CbteTipo", cbteTipo)),
                            new XElement(wsfeNs + "FeDetReq", detail))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private async Task<string> PostSoapAsync(string url, string envelope, string soapAction, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient(nameof(FiscalIntegrationService));
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        if (soapAction is not null)
            request.Headers.TryAddWithoutValidation("SOAPAction", soapAction);

        using var response = await client.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode && !LooksLikeSoapFault(responseText))
            throw new InvalidOperationException($"ARCA/AFIP devolvió HTTP {(int)response.StatusCode}: {Truncate(responseText, 500)}");

        return responseText;
    }

    private static AccessTicket ParseWsaaResponse(string soapXml)
    {
        var soap = XDocument.Parse(soapXml);
        ThrowIfSoapFault(soap, "WSAA");

        var loginCmsReturn = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "loginCmsReturn")?.Value;
        if (string.IsNullOrWhiteSpace(loginCmsReturn))
            throw new InvalidOperationException("WSAA no devolvió loginCmsReturn.");

        var ticket = XDocument.Parse(loginCmsReturn);
        var token = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "token")?.Value;
        var sign = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "sign")?.Value;
        var expirationTime = ticket.Descendants().FirstOrDefault(x => x.Name.LocalName == "expirationTime")?.Value;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(sign) || string.IsNullOrWhiteSpace(expirationTime))
            throw new InvalidOperationException("WSAA devolvió un Ticket de Acceso incompleto.");

        return new AccessTicket(
            token,
            sign,
            DateTime.Parse(expirationTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
            loginCmsReturn);
    }

    private FiscalAuthorizationResult ParseWsfeResponse(CommercialInvoice invoice, FiscalConfiguration configuration, string requestXml, string responseXml)
    {
        var soap = XDocument.Parse(responseXml);
        ThrowIfSoapFault(soap, "WSFEv1");

        var resultNode = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "FECAESolicitarResult");
        if (resultNode is null)
            throw new InvalidOperationException("WSFEv1 no devolvió FECAESolicitarResult.");

        var detail = resultNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "FECAEDetResponse");
        if (detail is null)
            throw new InvalidOperationException("WSFEv1 no devolvió FECAEDetResponse.");

        var resultCode = detail.Descendants().FirstOrDefault(x => x.Name.LocalName == "Resultado")?.Value
            ?? resultNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "Resultado")?.Value
            ?? string.Empty;
        var cae = detail.Descendants().FirstOrDefault(x => x.Name.LocalName == "CAE")?.Value;
        var caeDue = detail.Descendants().FirstOrDefault(x => x.Name.LocalName == "CAEFchVto")?.Value;
        var errors = ExtractAfipMessages(resultNode, "Errors", "Err");
        var observations = ExtractAfipMessages(detail, "Observaciones", "Obs");
        var detailMessage = string.Join(" | ", errors.Concat(observations).Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(detailMessage))
            detailMessage = resultCode == "A" ? "CAE autorizado correctamente por ARCA." : "ARCA devolvió el comprobante sin detalle adicional.";

        var status = resultCode switch
        {
            "A" => FiscalSubmissionStatus.Authorized,
            "R" => FiscalSubmissionStatus.Rejected,
            _ when errors.Count > 0 => FiscalSubmissionStatus.Error,
            _ => FiscalSubmissionStatus.Error
        };

        DateTime? caeDueDate = null;
        if (!string.IsNullOrWhiteSpace(caeDue) && DateTime.TryParseExact(caeDue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDue))
            caeDueDate = DateTime.SpecifyKind(parsedDue, DateTimeKind.Utc);

        return new FiscalAuthorizationResult(
            status,
            requestXml,
            responseXml,
            status == FiscalSubmissionStatus.Authorized ? null : detailMessage,
            status == FiscalSubmissionStatus.Authorized ? cae : null,
            status == FiscalSubmissionStatus.Authorized ? caeDueDate : null,
            status == FiscalSubmissionStatus.Authorized ? cae : null,
            detailMessage);
    }

    private static void ThrowIfSoapFault(XDocument soap, string serviceName)
    {
        var fault = soap.Descendants().FirstOrDefault(x => x.Name.LocalName == "Fault");
        if (fault is null)
            return;

        var faultCode = fault.Descendants().FirstOrDefault(x => x.Name.LocalName is "faultcode" or "Value")?.Value;
        var faultMessage = fault.Descendants().FirstOrDefault(x => x.Name.LocalName is "faultstring" or "Text" or "Reason")?.Value
            ?? fault.Value;
        var detailMessage = BuildSoapFaultDetailMessage(faultCode, faultMessage);
        throw new InvalidOperationException($"{serviceName} devolvió SOAP Fault: {detailMessage}");
    }

    private static List<string> ExtractAfipMessages(XElement root, string collectionName, string itemName)
        => root.Descendants()
            .Where(x => x.Name.LocalName == itemName && x.Parent?.Name.LocalName == collectionName)
            .Select(x =>
            {
                var code = x.Elements().FirstOrDefault(e => e.Name.LocalName == "Code")?.Value;
                var msg = x.Elements().FirstOrDefault(e => e.Name.LocalName == "Msg")?.Value;
                return string.IsNullOrWhiteSpace(code) ? msg ?? string.Empty : $"{code}: {msg}";
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

    private static string GetWsaaUrl(FiscalConfiguration configuration)
        => configuration.UseSandbox ? WsaaHomoUrl : WsaaProdUrl;

    private static string GetWsfeUrl(FiscalConfiguration configuration)
        => configuration.UseSandbox ? WsfeHomoUrl : WsfeProdUrl;

    private static long NormalizeCuit(string cuit)
    {
        var digits = new string((cuit ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || !long.TryParse(digits, out var value))
            throw new InvalidOperationException("El CUIT configurado para ARCA no es válido.");

        return value;
    }

    private static int ResolveCbteTipo(InvoiceType type) => type switch
    {
        InvoiceType.InvoiceA => 1,
        InvoiceType.InvoiceB => 6,
        InvoiceType.InvoiceC => 11,
        InvoiceType.CreditNoteA => 3,
        InvoiceType.CreditNoteB => 8,
        InvoiceType.CreditNoteC => 13,
        _ => 6
    };

    private static int ResolveDocumentType(string? documentNumber)
    {
        var digits = new string((documentNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length switch
        {
            11 => 80,
            8 => 96,
            7 => 96,
            _ => 99
        };
    }

    private static long ResolveDocumentNumber(string? documentNumber, int docType)
    {
        if (docType == 99)
            return 0;

        var digits = new string((documentNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out var value) ? value : 0;
    }

    private static int ResolveVatRateId(decimal taxRate)
    {
        var normalized = Math.Round(taxRate, 4, MidpointRounding.AwayFromZero);
        return normalized switch
        {
            0m => 3,
            0.105m => 4,
            0.21m => 5,
            0.27m => 6,
            0.05m => 8,
            0.025m => 9,
            _ => 5
        };
    }

    private static string ResolveCurrencyCode(string? currencyCode)
        => string.Equals(currencyCode, "USD", StringComparison.OrdinalIgnoreCase) ? "DOL" : "PES";

    private static string FormatDecimal(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static bool LooksLikeSoapFault(string value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains("<", StringComparison.Ordinal)
           && (value.Contains("Fault", StringComparison.OrdinalIgnoreCase)
               || value.Contains("Envelope", StringComparison.OrdinalIgnoreCase));

    private static string BuildSoapFaultDetailMessage(string? faultCode, string? faultMessage)
    {
        var normalizedCode = (faultCode ?? string.Empty).Trim();
        var normalizedMessage = (faultMessage ?? string.Empty).Trim();

        if (normalizedCode.Contains("cms.cert.untrusted", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("Certificado no emitido por AC de confianza", StringComparison.OrdinalIgnoreCase))
        {
            return "Certificado no emitido por AC de confianza. Verificá que el certificado haya sido emitido por ARCA y que coincida el ambiente: certificado de homologación con WSAA/WSFE de homologación, o certificado de producción con endpoints productivos.";
        }

        if (normalizedCode.Contains("xml.destination.invalid", StringComparison.OrdinalIgnoreCase))
        {
            return "El destination del LoginTicketRequest no coincide con el DN del WSAA. Revisá el ambiente configurado y el endpoint utilizado.";
        }

        if (string.IsNullOrWhiteSpace(normalizedCode))
            return normalizedMessage;

        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return normalizedCode;

        return $"{normalizedCode}: {normalizedMessage}";
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrWhiteSpace(value) || value.Length <= max ? value : value[..max];

    private sealed record AccessTicket(string Token, string Sign, DateTime ExpirationTimeUtc, string RawXml);
}
