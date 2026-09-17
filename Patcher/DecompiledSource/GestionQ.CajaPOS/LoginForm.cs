using System;
using System.Drawing;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;
using Microsoft.VisualBasic;

namespace GestionQ.CajaPOS;

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
		base.Size = new System.Drawing.Size(350, 300);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		System.Windows.Forms.Label value = new System.Windows.Forms.Label
		{
			Text = "Ingrese su PIN",
			Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
			Dock = System.Windows.Forms.DockStyle.Top,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Height = 60
		};
		base.Controls.Add(value);
		this.txtPin = new System.Windows.Forms.TextBox
		{
			Font = new System.Drawing.Font("Segoe UI", 24f),
			PasswordChar = '*',
			TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
			Width = 200,
			Location = new System.Drawing.Point(65, 80),
			MaxLength = 4
		};
		this.txtPin.KeyPress += delegate(object? s, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
			{
				e.Handled = true;
			}
		};
		this.txtPin.KeyDown += delegate(object? s, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == System.Windows.Forms.Keys.Return)
			{
				this.btnLogin.PerformClick();
			}
		};
		base.Controls.Add(this.txtPin);
		this.btnLogin = new System.Windows.Forms.Button
		{
			Text = "Ingresar",
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Width = 200,
			Height = 40,
			Location = new System.Drawing.Point(65, 140),
			BackColor = System.Drawing.Color.FromArgb(0, 122, 204),
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnLogin.Click += new System.EventHandler(BtnLogin_Click);
		base.Controls.Add(this.btnLogin);
		this.lblError = new System.Windows.Forms.Label
		{
			ForeColor = System.Drawing.Color.Red,
			Font = new System.Drawing.Font("Segoe UI", 9f),
			Width = 300,
			Location = new System.Drawing.Point(25, 190),
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		};
		base.Controls.Add(this.lblError);
		System.Windows.Forms.Button button = new System.Windows.Forms.Button
		{
			Text = "⚙\ufe0f Servidor",
			Font = new System.Drawing.Font("Segoe UI", 9f),
			AutoSize = true,
			Location = new System.Drawing.Point(10, 220),
			FlatStyle = System.Windows.Forms.FlatStyle.Flat,
			ForeColor = System.Drawing.Color.Gray
		};
		button.FlatAppearance.BorderSize = 0;
		button.Click += delegate
		{
			string serverUrl = GestionQ.CajaPOS.AppConfig.ServerUrl;
			string text = Microsoft.VisualBasic.Interaction.InputBox("Ingrese la URL o IP del Servidor Central (ej. http://192.168.1.10:5144):", "Configurar Servidor", serverUrl);
			if (!string.IsNullOrWhiteSpace(text) && text != serverUrl)
			{
				GestionQ.CajaPOS.AppConfig.ServerUrl = text;
				System.Windows.Forms.MessageBox.Show("La configuración se ha guardado. La aplicación se reiniciará para aplicar los cambios.", "Configuración", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Asterisk);
				System.Windows.Forms.Application.Restart();
			}
		};
		base.Controls.Add(button);
	}

	private async void BtnLogin_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtPin.Text))
		{
			return;
		}
		btnLogin.Enabled = false;
		lblError.Text = "Verificando...";
		try
		{
			PosLoginResponseDto posLoginResponseDto = await _authClient.LoginAsync(txtPin.Text, _posIdentifier);
			if (posLoginResponseDto.Success)
			{
				LoginResult = posLoginResponseDto;
				base.DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				lblError.Text = posLoginResponseDto.ErrorMessage ?? "Error desconocido.";
				txtPin.Clear();
				txtPin.Focus();
			}
		}
		catch (Exception)
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
