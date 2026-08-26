using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestionQ.Domain.Entities;

namespace GestionQ.Web.Services
{
    public class CashRegisterTicketGenerator
    {
        public static string GenerateXReport(CashRegister register)
        {
            var sb = new StringBuilder();

            string CenterText(string text, int width = 42)
            {
                if (string.IsNullOrEmpty(text)) return "";
                text = text.Trim();
                if (text.Length >= width) return text.Substring(0, width);
                int spaces = (width - text.Length) / 2;
                return text.PadLeft(text.Length + spaces).PadRight(width);
            }

            string FormatLine(string leftText, string rightText, int width = 42)
            {
                leftText = leftText ?? "";
                rightText = rightText ?? "";
                if (leftText.Length + rightText.Length > width)
                {
                    int maxLeft = width - rightText.Length - 1;
                    if (maxLeft > 0) leftText = leftText.Substring(0, maxLeft);
                    else leftText = "";
                }
                return leftText.PadRight(width - rightText.Length) + rightText;
            }
            
            string FormatCol3(string col1, string col2, string col3, int width = 42)
            {
                int col3Width = 12;
                int col2Width = 8;
                int col1Width = width - col2Width - col3Width;
                
                string c1 = col1.Length > col1Width ? col1.Substring(0, col1Width) : col1.PadRight(col1Width);
                string c2 = col2.PadLeft(col2Width);
                string c3 = col3.PadLeft(col3Width);
                return c1 + c2 + c3;
            }

            var sales = register.Sales?.Where(s => !s.IsCancelled).ToList() ?? new List<Sale>();
            var cancelledSales = register.Sales?.Where(s => s.IsCancelled).ToList() ?? new List<Sale>();

            var paymentSummary = sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod?.Name?.ToUpper() ?? "DESCONOCIDO")
                .Select(g => new { Method = g.Key, Count = g.Select(p => p.SaleId).Distinct().Count(), Total = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Method)
                .ToList();

            var facturas = sales.Where(s => s.ElectronicInvoice != null).ToList();
            var tickets = sales.Where(s => s.ElectronicInvoice == null).ToList();
            
            decimal totalFacturas = facturas.Sum(s => s.TotalAmount);
            decimal totalTickets = tickets.Sum(s => s.TotalAmount);
            decimal totalCancelados = cancelledSales.Sum(s => s.TotalAmount);

            decimal netoGravadoExento = 0;
            decimal totalIva = 0;
            var ivaBreakdown = new Dictionary<decimal, decimal>();

            foreach (var sale in sales)
            {
                if (sale.ElectronicInvoice != null)
                {
                    netoGravadoExento += sale.ElectronicInvoice.NetAmount + sale.ElectronicInvoice.ExemptAmount;
                    totalIva += sale.ElectronicInvoice.VatAmount;
                }
                
                foreach(var item in sale.Items)
                {
                    decimal rate = item.Product?.VatRate?.Rate ?? 21m;
                    decimal lineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
                    decimal ivaMultiplier = 1 + (rate / 100m);
                    decimal lineNeto = lineTotal / ivaMultiplier;
                    decimal lineIva = lineTotal - lineNeto;

                    if (sale.ElectronicInvoice == null) 
                    {
                        netoGravadoExento += lineNeto;
                        totalIva += lineIva;
                    }

                    if (!ivaBreakdown.ContainsKey(rate)) ivaBreakdown[rate] = 0;
                    ivaBreakdown[rate] += lineIva;
                }
            }
            decimal totalSalesAmount = sales.Sum(s => s.TotalAmount);

            var manualInflows = register.Movements?.Where(m => m.Type == "Ingreso").ToList() ?? new List<CashRegisterMovement>();
            var manualOutflows = register.Movements?.Where(m => m.Type == "Egreso").ToList() ?? new List<CashRegisterMovement>();
            decimal totalMovimientos = manualInflows.Sum(m => m.Amount) - manualOutflows.Sum(m => m.Amount);
            int cantMovimientos = manualInflows.Count + manualOutflows.Count;

            decimal totalEfectivoVentas = paymentSummary.FirstOrDefault(p => p.Method == "EFECTIVO")?.Total ?? 0;
            decimal calculatedExpectedCash = register.InitialBalance + totalEfectivoVentas + totalMovimientos;

            sb.AppendLine(FormatLine("Fecha/hora de impresión:", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            sb.AppendLine();
            sb.AppendLine(CenterText("PARCIAL DE CAJA"));
            sb.AppendLine(CenterText("INFORME X"));
            sb.AppendLine(new string('-', 42));
            sb.AppendLine(FormatCol3("medios de cobro", "veces", "monto"));
            sb.AppendLine();
            
            foreach (var p in paymentSummary)
            {
                sb.AppendLine(FormatCol3(p.Method, p.Count.ToString(), p.Total.ToString("N2")));
            }
            
            sb.AppendLine();
            sb.AppendLine(CenterText("Detalle de comprobantes", 42).Replace(" ", "-").Replace("Detalle-de-comprobantes", " Detalle de comprobantes "));
            
            if (tickets.Any())
                sb.AppendLine(FormatCol3("TICKET CONS. FINAL", tickets.Count.ToString(), totalTickets.ToString("N2")));
            if (facturas.Any())
                sb.AppendLine(FormatCol3("FACTURA B/C", facturas.Count.ToString(), totalFacturas.ToString("N2")));
                
            sb.AppendLine(new string('-', 42));
            sb.AppendLine(FormatCol3("Cptbes. cancelados", cancelledSales.Count.ToString(), $"$ {totalCancelados:N2}"));
            sb.AppendLine(FormatCol3("Anulación de items", "0", "$ 0,00"));
            sb.AppendLine(FormatCol3("Envases", "0", "$ 0,00"));
            sb.AppendLine(FormatCol3("Dev. envases", "0", "$ 0,00"));
            sb.AppendLine();
            
            sb.AppendLine(CenterText("Detalle de impuesto", 42).Replace(" ", "-").Replace("Detalle-de-impuesto", " Detalle de impuesto "));
            sb.AppendLine(FormatLine("NETO GRAVADO + EXENTO", netoGravadoExento.ToString("N2")));
            sb.AppendLine(FormatLine("Impuestos", $"$ {totalIva:N2}"));
            sb.AppendLine(new string('+', 42));
            
            foreach (var kvp in ivaBreakdown.OrderBy(k => k.Key))
            {
                if (kvp.Value > 0)
                    sb.AppendLine(FormatLine($"IVA {kvp.Key:0.##} %", $"$ {kvp.Value:N2}"));
            }
            sb.AppendLine(new string('=', 42));
            sb.AppendLine(FormatLine("(1)", totalSalesAmount.ToString("N2")));
            sb.AppendLine();
            
            sb.AppendLine(CenterText("Movimientos de caja", 42).Replace(" ", "-").Replace("Movimientos-de-caja", " Movimientos de caja "));
            if (cantMovimientos == 0)
            {
                sb.AppendLine(CenterText("sin movimientos"));
            }
            else
            {
                foreach(var m in manualInflows) sb.AppendLine(FormatLine($"+ {m.Description}", m.Amount.ToString("N2")));
                foreach(var m in manualOutflows) sb.AppendLine(FormatLine($"- {m.Description}", m.Amount.ToString("N2")));
            }
            sb.AppendLine(new string('-', 42));
            sb.AppendLine(FormatLine("(2)", $"$ {totalMovimientos:N2}"));
            sb.AppendLine();
            
            sb.AppendLine(CenterText("Resumen de operaciones", 42).Replace(" ", "-").Replace("Resumen-de-operaciones", " Resumen de operaciones "));
            sb.AppendLine(FormatLine("VENTAS", totalSalesAmount.ToString("N2")));
            sb.AppendLine(FormatLine("MOVIMIENTOS", $"$ {totalMovimientos:N2}"));
            sb.AppendLine(FormatLine("Cant. Operaciones", (sales.Count + cantMovimientos).ToString()));
            sb.AppendLine(new string('=', 42));
            sb.AppendLine(FormatLine("SALDO GENERAL (1) + (2)", (totalSalesAmount + totalMovimientos).ToString("N2")));
            sb.AppendLine();
            
            sb.AppendLine(CenterText("Medios de cobro en caja declarados", 42).Replace(" ", "-").Replace("Medios-de-cobro-en-caja-declarados", " Medios de cobro en caja declarados "));
            sb.AppendLine(FormatLine("medios de cobro", "en caja".PadRight(15) + "declarado"));
            sb.AppendLine();
            
            foreach (var p in paymentSummary)
            {
                if (p.Method == "EFECTIVO") continue;
                sb.AppendLine(FormatLine(p.Method, p.Total.ToString("N2").PadRight(15) + "$ 0,00"));
            }
            
            decimal efectivoEnCaja = calculatedExpectedCash;
            decimal efectivoDeclarado = register.FinalCashBalance ?? 0;
            sb.AppendLine(FormatLine("EFECTIVO", efectivoEnCaja.ToString("N2").PadRight(15) + efectivoDeclarado.ToString("N2")));
            sb.AppendLine(new string('-', 42));
            
            sb.AppendLine(FormatLine("SALDO (Diferencia Efectivo)", $"$ {(efectivoDeclarado - efectivoEnCaja):N2}"));
            
            return sb.ToString();
        }
    }
}
