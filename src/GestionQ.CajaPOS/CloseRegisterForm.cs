using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS
{
    public class CloseRegisterForm : Form
    {
        private AuthClient _authClient;
        private int _cashRegisterId;
        private TextBox txtFinalBalance;
        private Button btnClose;
        private Label lblError;

        public bool CloseSuccess { get; private set; }
        public string TicketText { get; private set; } = "";
        public string DifferenceTicketText { get; private set; } = "";

        public CloseRegisterForm(AuthClient authClient, int cashRegisterId)
        {
            _authClient = authClient;
            _cashRegisterId = cashRegisterId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Cierre de Caja";
            this.Size = new Size(350, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label { Text = "Efectivo en Caja ($)", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 60 };
            this.Controls.Add(lblTitle);

            txtFinalBalance = new TextBox { Font = new Font("Segoe UI", 24), TextAlign = HorizontalAlignment.Center, Width = 200, Location = new Point(65, 80) };
            txtFinalBalance.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.') e.Handled = true; };
            txtFinalBalance.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnClose.PerformClick(); };
            this.Controls.Add(txtFinalBalance);

            btnClose = new Button { Text = "Cerrar Caja", Font = new Font("Segoe UI", 12), Width = 200, Height = 40, Location = new Point(65, 140), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClose.Click += BtnClose_Click;
            this.Controls.Add(btnClose);

            lblError = new Label { ForeColor = Color.Red, Font = new Font("Segoe UI", 9), Width = 300, Location = new Point(25, 200), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblError);
        }

        private async void BtnClose_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFinalBalance.Text)) return;
            
            string amountStr = txtFinalBalance.Text.Replace(".", ",");
            if (!decimal.TryParse(amountStr, out decimal finalBalance))
            {
                lblError.Text = "Monto inválido.";
                return;
            }

            btnClose.Enabled = false;
            lblError.Text = "Cerrando caja...";

            try
            {
                using (var db = new LocalDbContext())
                {
                    var register = db.OfflineCashRegisters.Find(_cashRegisterId);
                    if (register != null)
                    {
                        register.ClosingDate = DateTime.Now;
                        register.FinalCashBalance = finalBalance;
                        register.IsSynced = false;
                        db.SaveChanges();
                        
                        var openingDate = register.OpeningDate;
                        var initialBalance = register.InitialBalance;
                        
                        var todasLasVentas = db.Sales.Where(s => s.CashRegisterId == _cashRegisterId && s.Date >= openingDate).ToList();
                        
                        var ventasActivas = todasLasVentas.Where(s => !s.IsCancelled).ToList();
                        var ventasAnuladas = todasLasVentas.Where(s => s.IsCancelled).ToList();
                        
                        var ventaIds = ventasActivas.Select(v => v.Id).ToList();
                        var pagos = db.SalePayments.Where(p => ventaIds.Contains(p.SaleId)).ToList();
                        var movimientos = db.Movements.Where(m => m.CashRegisterId == _cashRegisterId && m.Date >= openingDate).ToList();
                        
                        var methodsDict = db.PaymentMethods.ToDictionary(m => m.Id, m => m.Name);
                        
                        decimal totalVentas = ventasActivas.Sum(v => v.TotalAmount);
                        decimal totalAnuladas = ventasAnuladas.Sum(v => v.TotalAmount);
                        int cantVentas = ventasActivas.Count;
                        int cantAnuladas = ventasAnuladas.Count;
                        
                        decimal ingresosExtra = movimientos.Where(m => m.Type == "Ingreso").Sum(m => m.Amount);
                        decimal retiros = movimientos.Where(m => m.Type == "Egreso").Sum(m => m.Amount);
                        decimal ventasEfectivo = pagos.Where(p => (methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Efectivo") == "Efectivo").Sum(p => p.Amount);
                        decimal efectivoEsperado = initialBalance + ventasEfectivo + ingresosExtra - retiros;
                        decimal diferencia = finalBalance - efectivoEsperado;
                        
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("================================");
                        sb.AppendLine("         CIERRE DE CAJA (Z)     ");
                        sb.AppendLine("================================");
                        sb.AppendLine($"Fecha Cierre: {register.ClosingDate}");
                        sb.AppendLine($"Caja: #{_cashRegisterId}");
                        sb.AppendLine($"Apertura: {openingDate}");
                        sb.AppendLine("--------------------------------");
                        sb.AppendLine($"Fondo Inicial: {initialBalance:C2}");
                        sb.AppendLine($"Ingresos Extra: {ingresosExtra:C2}");
                        sb.AppendLine($"Retiros: {retiros:C2}");
                        sb.AppendLine("--------------------------------");
                        sb.AppendLine($"TOTAL VENTAS: {totalVentas:C2}");
                        sb.AppendLine($"Cant. Tickets Venta: {cantVentas}");
                        sb.AppendLine($"TOTAL ANULACIONES: {totalAnuladas:C2}");
                        sb.AppendLine($"Cant. Tickets Anulados: {cantAnuladas}");
                        sb.AppendLine("");
                        sb.AppendLine("MEDIOS DE PAGO:");
                        var groupedPayments = pagos.GroupBy(p => methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Desconocido").Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Amount) }).ToList();
                        foreach(var p in groupedPayments)
                        {
                            sb.AppendLine($"- {p.Metodo}: {p.Total:C2}");
                        }
                        sb.AppendLine("--------------------------------");
                        sb.AppendLine($"EFECTIVO ESPERADO:  {efectivoEsperado:C2}");
                        sb.AppendLine($"EFECTIVO DECLARADO: {finalBalance:C2}");
                        sb.AppendLine($"DIFERENCIA:         {diferencia:C2}");
                        sb.AppendLine("================================");
                        sb.AppendLine("");
                        sb.AppendLine("");
                        
                        TicketText = sb.ToString();

                        if (diferencia != 0)
                        {
                            var sbDiff = new System.Text.StringBuilder();
                            sbDiff.AppendLine("================================");
                            sbDiff.AppendLine("   COMPROBANTE DE DIFERENCIA    ");
                            sbDiff.AppendLine("================================");
                            sbDiff.AppendLine($"Fecha Cierre: {register.ClosingDate}");
                            sbDiff.AppendLine($"Caja: #{_cashRegisterId}");
                            sbDiff.AppendLine("--------------------------------");
                            sbDiff.AppendLine($"EFECTIVO ESPERADO:  {efectivoEsperado:C2}");
                            sbDiff.AppendLine($"EFECTIVO DECLARADO: {finalBalance:C2}");
                            sbDiff.AppendLine($"DIFERENCIA:         {diferencia:C2}");
                            sbDiff.AppendLine("--------------------------------");
                            sbDiff.AppendLine("");
                            sbDiff.AppendLine("");
                            sbDiff.AppendLine("Firma Cajero: __________________");
                            sbDiff.AppendLine("");
                            sbDiff.AppendLine("Aclaracion: ____________________");
                            sbDiff.AppendLine("");
                            sbDiff.AppendLine("================================");
                            sbDiff.AppendLine("");
                            sbDiff.AppendLine("");
                            
                            DifferenceTicketText = sbDiff.ToString();
                        }
                    }
                }
                CloseSuccess = true;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                lblError.Text = "Error al cerrar la caja localmente: " + ex.Message;
            }
            finally
            {
                btnClose.Enabled = true;
            }
        }
    }
}
