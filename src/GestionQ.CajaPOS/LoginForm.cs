using System;
using System.Drawing;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS
{
    public class LoginForm : Form
    {
        private AuthClient _authClient;
        private string _posIdentifier;
        private TextBox txtPin;
        private Button btnLogin;
        private Label lblError;

        public PosLoginResponseDto LoginResult { get; private set; }

        public LoginForm(AuthClient authClient, string posIdentifier)
        {
            _authClient = authClient;
            _posIdentifier = posIdentifier;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Login de Cajero";
            this.Size = new Size(350, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label { Text = "Ingrese su PIN", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 60 };
            this.Controls.Add(lblTitle);

            txtPin = new TextBox { Font = new Font("Segoe UI", 24), PasswordChar = '*', TextAlign = HorizontalAlignment.Center, Width = 200, Location = new Point(65, 80), MaxLength = 4 };
            txtPin.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtPin.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnLogin.PerformClick(); };
            this.Controls.Add(txtPin);

            btnLogin = new Button { Text = "Ingresar", Font = new Font("Segoe UI", 12), Width = 200, Height = 40, Location = new Point(65, 140), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            lblError = new Label { ForeColor = Color.Red, Font = new Font("Segoe UI", 9), Width = 300, Location = new Point(25, 200), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblError);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPin.Text)) return;

            btnLogin.Enabled = false;
            lblError.Text = "Verificando...";

            try
            {
                var result = await _authClient.LoginAsync(txtPin.Text, _posIdentifier);
                if (result.Success)
                {
                    LoginResult = result;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.Text = result.ErrorMessage ?? "Error desconocido.";
                    txtPin.Clear();
                    txtPin.Focus();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error de conexión con el servidor central.";
                txtPin.Clear();
                txtPin.Focus();
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }
}
