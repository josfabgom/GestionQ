using System;
using System.Drawing;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS
{
    public class OpenRegisterForm : Form
    {
        private AuthClient _authClient;
        private string _userId;
        private string _posIdentifier;
        private TextBox txtInitialBalance;
        private Button btnOpen;
        private Label lblError;

        public PosOpenRegisterResponseDto OpenResult { get; private set; }

        public OpenRegisterForm(AuthClient authClient, string userId, string posIdentifier)
        {
            _authClient = authClient;
            _userId = userId;
            _posIdentifier = posIdentifier;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Apertura de Caja";
            this.Size = new Size(350, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label { Text = "Saldo Inicial ($)", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 60 };
            this.Controls.Add(lblTitle);

            txtInitialBalance = new TextBox { Font = new Font("Segoe UI", 24), TextAlign = HorizontalAlignment.Center, Width = 200, Location = new Point(65, 80) };
            txtInitialBalance.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.') e.Handled = true; };
            txtInitialBalance.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnOpen.PerformClick(); };
            this.Controls.Add(txtInitialBalance);

            btnOpen = new Button { Text = "Abrir Caja", Font = new Font("Segoe UI", 12), Width = 200, Height = 40, Location = new Point(65, 140), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOpen.Click += BtnOpen_Click;
            this.Controls.Add(btnOpen);

            lblError = new Label { ForeColor = Color.Red, Font = new Font("Segoe UI", 9), Width = 300, Location = new Point(25, 200), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblError);
        }

        private async void BtnOpen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInitialBalance.Text)) return;
            
            string amountStr = txtInitialBalance.Text.Replace(".", ",");
            if (!decimal.TryParse(amountStr, out decimal initialBalance))
            {
                lblError.Text = "Monto inválido.";
                return;
            }

            btnOpen.Enabled = false;
            lblError.Text = "Abriendo caja...";

            try
            {
                var result = await _authClient.OpenRegisterAsync(_userId, _posIdentifier, initialBalance);
                if (result.Success)
                {
                    OpenResult = result;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.Text = result.ErrorMessage ?? "Error desconocido.";
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error de conexión con el servidor central.";
            }
            finally
            {
                btnOpen.Enabled = true;
            }
        }
    }
}
