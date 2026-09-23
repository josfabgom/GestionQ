using GestionQ.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace GestionQ.Infrastructure.Services
{
    public class InvoicePdfGenerator : IInvoicePdfGenerator
    {
        public byte[] GeneratePdf(ElectronicInvoice invoice, string companyName, string companyCuit, string companyCondition, string companyAddress)
        {
            // Register QuestPDF community license to avoid watermark exception
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, invoice, companyName, companyCuit, companyCondition, companyAddress));
                    page.Content().Element(c => ComposeContent(c, invoice));
                    page.Footer().Element(c => ComposeFooter(c, invoice));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, ElectronicInvoice invoice, string companyName, string companyCuit, string companyCondition, string companyAddress)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(companyName).FontSize(20).SemiBold();
                    column.Item().Text(companyAddress);
                    column.Item().Text($"Condición frente al IVA: {companyCondition}");
                });
                
                row.ConstantItem(100).AlignCenter().Text(invoice.InvoiceTypeDesc.Replace("Factura ", "")).FontSize(30).Bold();
                
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("FACTURA").FontSize(20).SemiBold().AlignRight();
                    column.Item().Text($"Nro: {invoice.FormattedVoucherNumber}").AlignRight();
                    column.Item().Text($"Fecha: {invoice.IssueDate:dd/MM/yyyy}").AlignRight();
                    column.Item().Text($"CUIT: {companyCuit}").AlignRight();
                });
            });
        }

        private void ComposeContent(IContainer container, ElectronicInvoice invoice)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column => 
            {
                // Customer details
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text($"Cliente: {invoice.CustomerName}").SemiBold();
                    row.RelativeItem().Text($"Condición: {invoice.CustomerTaxCondition}");
                    row.RelativeItem().Text($"{(invoice.DocTypeCode == 80 ? "CUIT" : "DNI")}: {invoice.DocNumber}").AlignRight();
                });

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Text("#").SemiBold();
                        header.Cell().Text("Producto").SemiBold();
                        header.Cell().AlignRight().Text("Cantidad").SemiBold();
                        header.Cell().AlignRight().Text("Precio Unit.").SemiBold();
                        header.Cell().AlignRight().Text("Descuento").SemiBold();
                        header.Cell().AlignRight().Text("Subtotal").SemiBold();
                    });

                    // Lines
                    if (invoice.Sale != null && invoice.Sale.Items != null)
                    {
                        int index = 1;
                        foreach (var item in invoice.Sale.Items)
                        {
                            var productName = item.Product?.Name ?? item.CustomName ?? "Producto";
                            table.Cell().Text(index.ToString());
                            table.Cell().Text(productName);
                            table.Cell().AlignRight().Text(item.Quantity.ToString("N2"));
                            table.Cell().AlignRight().Text($"$ {item.UnitPrice:N2}");
                            table.Cell().AlignRight().Text($"$ {item.DiscountAmount:N2}");
                            table.Cell().AlignRight().Text($"$ {(item.Quantity * item.UnitPrice - item.DiscountAmount):N2}");
                            index++;
                        }
                    }
                });
                
                // Totals
                column.Item().PaddingTop(20).AlignRight().Column(totals => 
                {
                    totals.Item().Text($"Subtotal Neto: $ {invoice.NetAmount:N2}");
                    totals.Item().Text($"IVA: $ {invoice.VatAmount:N2}");
                    totals.Item().Text($"Exento: $ {invoice.ExemptAmount:N2}");
                    if (invoice.Sale?.PaymentDiscountAmount > 0)
                        totals.Item().Text($"Desc. Pago: -$ {invoice.Sale.PaymentDiscountAmount:N2}");
                        
                    totals.Item().Text($"Total: $ {invoice.TotalAmount:N2}").FontSize(14).SemiBold();
                });
            });
        }

        private void ComposeFooter(IContainer container, ElectronicInvoice invoice)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col => 
                    {
                        col.Item().Text($"CAE: {invoice.CAE}").SemiBold();
                        col.Item().Text($"Vencimiento CAE: {invoice.CAEExpirationDate:dd/MM/yyyy}");
                    });
                    
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        }
    }
}
