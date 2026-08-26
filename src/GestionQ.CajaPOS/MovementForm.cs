using System;
using System.Drawing;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS
{
    public class MovementForm : Form
    {
        private AuthClient _authClient;
        private int _cashRegisterId;
        private TextBox txtAmount;
        private TextBox txtDescription;
        private Button btnSave;
        private Label lblError;

        public bool SaveSuccess { get; private set; }
        public decimal Amount => decimal.TryParse(txtAmount.Text.Replace(".", ","), out var a) ? a : 0m;
        public string Description => txtDescription.Text;

        public MovementForm(AuthClient authClient, int cashRegisterId)
        {
            _authClient = authClient;
            _cashRegisterId = cashRegisterId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Retiro de Efectivo";
            this.Size = new Size(400, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label { Text = "Nuevo Egreso", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 50 };
            this.Controls.Add(lblTitle);

            Label lblAmount = new Label { Text = "Monto a Retirar ($):", Font = new Font("Segoe UI", 10), Location = new Point(50, 70), Width = 300 };
            this.Controls.Add(lblAmount);

            txtAmount = new TextBox { Font = new Font("Segoe UI", 20), TextAlign = HorizontalAlignment.Center, Width = 280, Location = new Point(50, 100) };
            txtAmount.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.') e.Handled = true; };
            txtAmount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtDescription.Focus(); };
            this.Controls.Add(txtAmount);

            Label lblDesc = new Label { Text = "Motivo (Ej. Pago a Proveedor):", Font = new Font("Segoe UI", 10), Location = new Point(50, 160), Width = 300 };
            this.Controls.Add(lblDesc);

            txtDescription = new TextBox { Font = new Font("Segoe UI", 12), Width = 280, Location = new Point(50, 190) };
            txtDescription.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSave.PerformClick(); };
            this.Controls.Add(txtDescription);

            btnSave = new Button { Text = "Registrar Retiro", Font = new Font("Segoe UI", 12), Width = 280, Height = 40, Location = new Point(50, 240), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            lblError = new Label { ForeColor = Color.Red, Font = new Font("Segoe UI", 9), Width = 380, Location = new Point(0, 290), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblError);
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAmount.Text) || string.IsNullOrWhiteSpace(txtDescription.Text)) 
            {
                lblError.Text = "Debe ingresar el monto y el motivo.";
                return;
            }
            
            string amountStr = txtAmount.Text.Replace(".", ",");
            if (!decimal.TryParse(amountStr, out decimal amount) || amount <= 0)
            {
                lblError.Text = "Monto inválido.";
                return;
            }

            btnSave.Enabled = false;
            lblError.Text = "Registrando movimiento...";

            try
            {
                var (success, errorMsg) = await _authClient.AddMovementAsync(_cashRegisterId, amount, txtDescription.Text, "Egreso");
                if (success)
                {
                    SaveSuccess = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.Text = errorMsg;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error de red con el servidor central.";
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }
}
