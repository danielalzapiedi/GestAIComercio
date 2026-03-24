using GestAI.Domain.Common;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class FiscalDocumentSubmission : AuditableEntity
{
    public int AccountId { get; set; }
    public int CommercialInvoiceId { get; set; }
    public CommercialInvoice CommercialInvoice { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public FiscalSubmissionStatus Status { get; set; } = FiscalSubmissionStatus.Pending;
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string RequestPayload { get; set; } = string.Empty;
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalReference { get; set; }
}
