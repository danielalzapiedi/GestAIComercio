using System.Globalization;
using GestAI.Application.Abstractions;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestAI.Infrastructure.Commerce;

public sealed class CommercialDocumentPdfService : ICommercialDocumentPdfService
{
    private const string PdfContentType = "application/pdf";

    public Task<DocumentFileResult> BuildInvoicePdfAsync(CommercialInvoice invoice, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => BuildHeader(c, "Factura", invoice.Number, InvoiceStatusLabel(invoice.Status)));
                page.Content().Element(c => BuildInvoiceContent(c, invoice));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado por GestAI Comercio · ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(new DocumentFileResult($"{SanitizeFileName(invoice.Number)}.pdf", bytes, PdfContentType));
    }

    public Task<DocumentFileResult> BuildDeliveryNotePdfAsync(DeliveryNote note, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => BuildHeader(c, "Remito", note.Number, DeliveryStatusLabel(note.Status)));
                page.Content().Element(c => BuildDeliveryNoteContent(c, note));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado por GestAI Comercio · ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(new DocumentFileResult($"{SanitizeFileName(note.Number)}.pdf", bytes, PdfContentType));
    }

    private static void BuildHeader(IContainer container, string title, string documentNumber, string status)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("GestAI Comercio").SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    left.Item().Text(title).Bold().FontSize(14);
                    left.Item().Text(documentNumber).FontSize(12);
                });

                row.ConstantItem(160).AlignRight().Column(right =>
                {
                    right.Item().Text($"Estado: {status}").SemiBold();
                    right.Item().Text($"Emitido: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}");
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void BuildInvoiceContent(IContainer container, CommercialInvoice invoice)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(c => BuildSectionCard(c, "Datos generales", info =>
            {
                info.Item().Element(i => BuildTwoColumnInfo(i,
                    ("Cliente", invoice.Customer.Name),
                    ("Venta origen", invoice.Sale.Number),
                    ("Tipo", InvoiceTypeLabel(invoice.InvoiceType)),
                    ("Punto de venta", invoice.PointOfSale.ToString(CultureInfo.InvariantCulture)),
                    ("Fecha", invoice.IssuedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")),
                    ("Moneda", invoice.CurrencyCode)));
            }));

            column.Item().Element(c => BuildSectionCard(c, "Datos fiscales", info =>
            {
                info.Item().Element(i => BuildTwoColumnInfo(i,
                    ("CAE", invoice.Cae ?? "Pendiente"),
                    ("Vencimiento CAE", invoice.CaeDueDateUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "-"),
                    ("Estado ARCA", InvoiceStatusLabel(invoice.Status)),
                    ("Detalle fiscal", string.IsNullOrWhiteSpace(invoice.FiscalStatusDetail) ? "-" : invoice.FiscalStatusDetail)));
            }));

            column.Item().Element(c => BuildSectionCard(c, "Detalle", info =>
            {
                info.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.6f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "Descripción");
                        HeaderCell(header, "Cant.");
                        HeaderCell(header, "Precio");
                        HeaderCell(header, "IVA");
                        HeaderCell(header, "Subtotal");
                    });

                    foreach (var item in invoice.Items.OrderBy(x => x.SortOrder))
                    {
                        BodyCell(table, $"{item.Description}\n{item.InternalCode}");
                        BodyCell(table, item.Quantity.ToString("0.##", CultureInfo.InvariantCulture));
                        BodyCell(table, FormatMoney(item.UnitPrice));
                        BodyCell(table, FormatMoney(item.TaxAmount));
                        BodyCell(table, FormatMoney(item.LineSubtotal));
                    }
                });
            }));

            column.Item().AlignRight().Width(240).Element(c => BuildTotalsCard(c,
                ("Subtotal", FormatMoney(invoice.Subtotal), false),
                ("IVA", FormatMoney(invoice.TaxAmount), false),
                ("Otros impuestos", FormatMoney(invoice.OtherTaxesAmount), false),
                ("Total", FormatMoney(invoice.Total), true)));

            if (invoice.DeliveryNotes.Any())
            {
                column.Item().Element(c => BuildSectionCard(c, "Remitos vinculados", info =>
                {
                    foreach (var note in invoice.DeliveryNotes.OrderByDescending(x => x.DeliveredAtUtc))
                        info.Item().Text($"• {note.Number} · {note.DeliveredAtUtc.ToLocalTime():dd/MM/yyyy}");
                }));
            }
        });
    }

    private static void BuildDeliveryNoteContent(IContainer container, DeliveryNote note)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(c => BuildSectionCard(c, "Datos generales", info =>
            {
                info.Item().Element(i => BuildTwoColumnInfo(i,
                    ("Cliente", note.Customer.Name),
                    ("Venta origen", note.Sale.Number),
                    ("Depósito", note.Warehouse.Name),
                    ("Fecha de entrega", note.DeliveredAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")),
                    ("Factura asociada", note.CommercialInvoice?.Number ?? "Sin factura"),
                    ("Observaciones", string.IsNullOrWhiteSpace(note.Observations) ? "-" : note.Observations)));
            }));

            column.Item().Element(c => BuildSectionCard(c, "Detalle entregado", info =>
            {
                info.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "Descripción");
                        HeaderCell(header, "Pedida");
                        HeaderCell(header, "Entregada");
                        HeaderCell(header, "Pendiente");
                    });

                    foreach (var item in note.Items.OrderBy(x => x.SortOrder))
                    {
                        BodyCell(table, $"{item.Description}\n{item.InternalCode}");
                        BodyCell(table, item.QuantityOrdered.ToString("0.##", CultureInfo.InvariantCulture));
                        BodyCell(table, item.QuantityDelivered.ToString("0.##", CultureInfo.InvariantCulture));
                        BodyCell(table, (item.QuantityOrdered - item.QuantityDelivered).ToString("0.##", CultureInfo.InvariantCulture));
                    }
                });
            }));

            column.Item().AlignRight().Width(240).Element(c => BuildTotalsCard(c,
                ("Total remitido", note.TotalQuantity.ToString("0.##", CultureInfo.InvariantCulture), false),
                ("Pendiente", note.PendingQuantity.ToString("0.##", CultureInfo.InvariantCulture), true)));
        });
    }

    private static void BuildSectionCard(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text(title).SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                content(column);
            });
    }

    private static void BuildTwoColumnInfo(IContainer container, params (string Label, string Value)[] items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            foreach (var pair in items.Chunk(2))
            {
                foreach (var item in pair)
                {
                    table.Cell().PaddingBottom(8).PaddingRight(8).Column(column =>
                    {
                        column.Item().Text(item.Label).SemiBold().FontColor(Colors.Grey.Darken2);
                        column.Item().Text(item.Value ?? "-");
                    });
                }

                if (pair.Length == 1)
                    table.Cell();
            }
        });
    }

    private static void BuildTotalsCard(IContainer container, params (string Label, string Value, bool Emphasize)[] rows)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(6);

                foreach (var row in rows)
                {
                    column.Item().Row(r =>
                    {
                        r.RelativeItem().Text(row.Label).SemiBold();
                        var text = r.ConstantItem(90).AlignRight().Text(row.Value);
                        if (row.Emphasize)
                            text.SemiBold().FontSize(12);
                    });
                }
            });
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text(text).SemiBold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(4).Text(text);
    }

    private static string FormatMoney(decimal value)
        => value.ToString("C", CultureInfo.GetCultureInfo("es-AR"));

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
    }

    private static string InvoiceTypeLabel(InvoiceType type) => type switch
    {
        InvoiceType.InvoiceA => "Factura A",
        InvoiceType.InvoiceB => "Factura B",
        InvoiceType.InvoiceC => "Factura C",
        InvoiceType.CreditNoteA => "Nota de crédito A",
        InvoiceType.CreditNoteB => "Nota de crédito B",
        InvoiceType.CreditNoteC => "Nota de crédito C",
        _ => type.ToString()
    };

    private static string InvoiceStatusLabel(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Borrador",
        InvoiceStatus.PendingAuthorization => "Pendiente",
        InvoiceStatus.Authorized => "Autorizada",
        InvoiceStatus.Rejected => "Rechazada",
        InvoiceStatus.IntegrationError => "Error de integración",
        InvoiceStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    private static string DeliveryStatusLabel(DeliveryNoteStatus status) => status switch
    {
        DeliveryNoteStatus.Issued => "Emitido",
        DeliveryNoteStatus.PartiallyDelivered => "Parcial",
        DeliveryNoteStatus.Delivered => "Entregado",
        DeliveryNoteStatus.Cancelled => "Cancelado",
        _ => "Borrador"
    };
}
