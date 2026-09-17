using System;
using System.Drawing;
using System.Windows.Forms;
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
                    }
                }
                CloseSuccess = true;
                
                // Let's not try to get the Ticket from the server synchronously here anymore.
                // It can be generated locally or skipped for now.
                TicketText = "Caja cerrada offline. Sincronizando con el servidor...";

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
