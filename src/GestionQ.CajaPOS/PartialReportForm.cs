using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;

namespace GestionQ.CajaPOS
{
    public class PartialReportForm : Form
    {
        private int _cashRegisterId;
        private string _userName;
        private DateTime _openingDate;
        private decimal _initialBalance;
        private LocalDbContext _db;

        private Label lblTotalVentas;
        private Label lblEfectivoEsperado;
        private DataGridView gridMethods;
        private DataGridView gridProducts;
        private Button btnPrint;
        private Button btnClose;

        public PartialReportForm(int cashRegisterId, string userName, DateTime openingDate, decimal initialBalance)
        {
            _cashRegisterId = cashRegisterId;
            _userName = userName;
            _openingDate = openingDate;
            _initialBalance = initialBalance;
            _db = new LocalDbContext();
            
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Reporte Parcial (Corte X)";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var titlePanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(16, 185, 129) };
            var lblTitle = new Label { Text = $"CORTE PARCIAL - Caja #{_cashRegisterId} ({_userName})", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            titlePanel.Controls.Add(lblTitle);
            this.Controls.Add(titlePanel);

            var summaryPanel = new Panel { Location = new Point(20, 80), Size = new Size(350, 420), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var lblSumTitle = new Label { Text = "Resumen de Caja", Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            lblTotalVentas = new Label { Font = new Font("Segoe UI", 12), Location = new Point(10, 45), AutoSize = true };
            lblEfectivoEsperado = new Label { Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.Green, Location = new Point(10, 120), AutoSize = true };
            
            summaryPanel.Controls.Add(lblSumTitle);
            summaryPanel.Controls.Add(lblTotalVentas);
            summaryPanel.Controls.Add(lblEfectivoEsperado);

            var lblMethods = new Label { Text = "Medios de Pago:", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(10, 155), AutoSize = true };
            summaryPanel.Controls.Add(lblMethods);

            gridMethods = new DataGridView { 
                Location = new Point(10, 185), Size = new Size(330, 220), 
                AllowUserToAddRows = false, ReadOnly = true, 
                RowHeadersVisible = false, AllowUserToResizeColumns = false, 
                AllowUserToResizeRows = false, BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            summaryPanel.Controls.Add(gridMethods);
            this.Controls.Add(summaryPanel);

            var productsPanel = new Panel { Location = new Point(390, 80), Size = new Size(370, 420), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var lblProdTitle = new Label { Text = "Artículos Vendidos", Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            productsPanel.Controls.Add(lblProdTitle);

            gridProducts = new DataGridView { 
                Location = new Point(10, 45), Size = new Size(350, 360), 
                AllowUserToAddRows = false, ReadOnly = true, 
                RowHeadersVisible = false, AllowUserToResizeColumns = false, 
                AllowUserToResizeRows = false, BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            productsPanel.Controls.Add(gridProducts);
            this.Controls.Add(productsPanel);

            btnPrint = new Button { Text = "🖨️ Imprimir Ticket", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(390, 510), Size = new Size(180, 40), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnPrint.Click += BtnPrint_Click;
            this.Controls.Add(btnPrint);

            btnClose = new Button { Text = "Cerrar", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(580, 510), Size = new Size(180, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void LoadData()
        {
            try
            {
                var ventas = _db.Sales
                    .Where(s => s.CashRegisterId == _cashRegisterId && s.Date >= _openingDate)
                    .ToList();

                var ventaIds = ventas.Select(v => v.Id).ToList();

                var pagos = _db.SalePayments
                    .Where(p => ventaIds.Contains(p.SaleId))
                    .ToList();
                    
                var items = _db.SaleItems
                    .Where(i => ventaIds.Contains(i.SaleId))
                    .ToList();

                var movimientos = _db.Movements
                    .Where(m => m.CashRegisterId == _cashRegisterId && m.Date >= _openingDate)
                    .ToList();

                // Join manual para nombres de método de pago y productos (ya que en SQLite local se ignoran las nav props)
                var methodsDict = _db.PaymentMethods.ToDictionary(m => m.Id, m => m.Name);
                var productsDict = _db.Products.ToDictionary(p => p.Id, p => p.Name);

                decimal totalVentas = ventas.Sum(v => v.TotalAmount);
                decimal ingresosExtra = movimientos.Where(m => m.Type == "IN").Sum(m => m.Amount);
                decimal retiros = movimientos.Where(m => m.Type == "OUT").Sum(m => m.Amount);
                
                decimal ventasEfectivo = pagos.Where(p => (methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Efectivo") == "Efectivo").Sum(p => p.Amount);
                
                decimal efectivoEsperado = _initialBalance + ventasEfectivo + ingresosExtra - retiros;

                lblTotalVentas.Text = $"Fondo Inicial: {_initialBalance:C2}\nTotal Ventas: {totalVentas:C2}\nIngresos: {ingresosExtra:C2} | Retiros: {retiros:C2}";
                lblEfectivoEsperado.Text = $"Efectivo Esperado: {efectivoEsperado:C2}";

                var groupedPayments = pagos.GroupBy(p => methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Desconocido")
                                         .Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Amount) })
                                         .ToList();
                gridMethods.DataSource = groupedPayments;
                if(gridMethods.Columns.Count > 0)
                {
                    gridMethods.Columns["Total"].DefaultCellStyle.Format = "C2";
                }

                var groupedItems = items.GroupBy(i => productsDict.ContainsKey(i.ProductId) ? productsDict[i.ProductId] : "Producto " + i.ProductId)
                                        .Select(g => new { Producto = g.Key, Cant = g.Sum(i => i.Quantity), Total = g.Sum(i => i.UnitPrice * i.Quantity) })
                                        .OrderByDescending(g => g.Cant)
                                        .ToList();
                
                gridProducts.DataSource = groupedItems;
                if(gridProducts.Columns.Count > 0)
                {
                    gridProducts.Columns["Producto"].Width = 180;
                    gridProducts.Columns["Cant"].Width = 60;
                    gridProducts.Columns["Total"].Width = 90;
                    gridProducts.Columns["Total"].DefaultCellStyle.Format = "C2";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                var ventas = _db.Sales.Where(s => s.CashRegisterId == _cashRegisterId && s.Date >= _openingDate && !s.IsCancelled).ToList();
                var ventaIds = ventas.Select(v => v.Id).ToList();
                var pagos = _db.SalePayments.Where(p => ventaIds.Contains(p.SaleId)).ToList();
                var movimientos = _db.Movements.Where(m => m.CashRegisterId == _cashRegisterId && m.Date >= _openingDate).ToList();
                
                var methodsDict = _db.PaymentMethods.ToDictionary(m => m.Id, m => m.Name);

                decimal totalVentas = ventas.Sum(v => v.TotalAmount);
                decimal ingresosExtra = movimientos.Where(m => m.Type == "IN").Sum(m => m.Amount);
                decimal retiros = movimientos.Where(m => m.Type == "OUT").Sum(m => m.Amount);
                decimal ventasEfectivo = pagos.Where(p => (methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Efectivo") == "Efectivo").Sum(p => p.Amount);
                decimal efectivoEsperado = _initialBalance + ventasEfectivo + ingresosExtra - retiros;

                var sb = new StringBuilder();
                sb.AppendLine("================================");
                sb.AppendLine("         CORTE PARCIAL (X)      ");
                sb.AppendLine("================================");
                sb.AppendLine($"Fecha: {DateTime.Now}");
                sb.AppendLine($"Caja: #{_cashRegisterId}");
                sb.AppendLine($"Cajero: {_userName}");
                sb.AppendLine($"Apertura: {_openingDate}");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"Fondo Inicial: {_initialBalance:C2}");
                sb.AppendLine($"Ingresos Extra: {ingresosExtra:C2}");
                sb.AppendLine($"Retiros: {retiros:C2}");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"TOTAL VENTAS: {totalVentas:C2}");
                sb.AppendLine("");
                sb.AppendLine("MEDIOS DE PAGO:");
                var groupedPayments = pagos.GroupBy(p => methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Desconocido").Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Amount) }).ToList();
                foreach(var p in groupedPayments)
                {
                    sb.AppendLine($"- {p.Metodo}: {p.Total:C2}");
                }
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"EFECTIVO ESPERADO: {efectivoEsperado:C2}");
                sb.AppendLine("================================");
                sb.AppendLine("");
                sb.AppendLine("");
                
                PrintTicket(sb.ToString());
                MessageBox.Show("Ticket de corte enviado a imprimir.", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintTicket(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                var pd = new System.Drawing.Printing.PrintDocument();
                
                string printerName = "";
                using (var db = new LocalDbContext())
                {
                    var pSetting = db.SystemSettings.FirstOrDefault(s => s.Key == "TicketPrinter");
                    if (pSetting != null) printerName = pSetting.Value;
                }
                if (!string.IsNullOrEmpty(printerName))
                {
                    pd.PrinterSettings.PrinterName = printerName;
                }
                
                pd.PrintPage += (s, ev) =>
                {
                    var font = new Font("Courier New", 8, FontStyle.Bold);
                    var brush = new SolidBrush(Color.Black);
                    
                    float yPos = 10;
                    float leftMargin = 0;
                    if (ev.Graphics == null) return;
                    
                    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach(var l in lines)
                    {
                        ev.Graphics.DrawString(l, font, brush, leftMargin, yPos);
                        yPos += font.GetHeight(ev.Graphics);
                    }
                };
                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de impresión: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
