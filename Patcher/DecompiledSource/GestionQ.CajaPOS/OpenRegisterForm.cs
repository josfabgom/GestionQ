using System;
using System.Drawing;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS;

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
		base.Size = new System.Drawing.Size(350, 300);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		System.Windows.Forms.Label value = new System.Windows.Forms.Label
		{
			Text = "Saldo Inicial ($)",
			Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
			Dock = System.Windows.Forms.DockStyle.Top,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Height = 60
		};
		base.Controls.Add(value);
		this.txtInitialBalance = new System.Windows.Forms.TextBox
		{
			Font = new System.Drawing.Font("Segoe UI", 24f),
			TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
			Width = 200,
			Location = new System.Drawing.Point(65, 80)
		};
		this.txtInitialBalance.KeyPress += delegate(object? s, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
			{
				e.Handled = true;
			}
		};
		this.txtInitialBalance.KeyDown += delegate(object? s, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == System.Windows.Forms.Keys.Return)
			{
				this.btnOpen.PerformClick();
			}
		};
		base.Controls.Add(this.txtInitialBalance);
		this.btnOpen = new System.Windows.Forms.Button
		{
			Text = "Abrir Caja",
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Width = 200,
			Height = 40,
			Location = new System.Drawing.Point(65, 140),
			BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnOpen.Click += new System.EventHandler(BtnOpen_Click);
		base.Controls.Add(this.btnOpen);
		this.lblError = new System.Windows.Forms.Label
		{
			ForeColor = System.Drawing.Color.Red,
			Font = new System.Drawing.Font("Segoe UI", 9f),
			Width = 300,
			Location = new System.Drawing.Point(25, 200),
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		};
		base.Controls.Add(this.lblError);
	}

	private async void BtnOpen_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtInitialBalance.Text))
		{
			return;
		}
		if (!decimal.TryParse(txtInitialBalance.Text.Replace(".", ","), out var result))
		{
			lblError.Text = "Monto inválido.";
			return;
		}
		btnOpen.Enabled = false;
		lblError.Text = "Abriendo caja...";
		try
		{
			PosOpenRegisterResponseDto posOpenRegisterResponseDto = await _authClient.OpenRegisterAsync(_userId, _posIdentifier, result);
			if (posOpenRegisterResponseDto.Success)
			{
				OpenResult = posOpenRegisterResponseDto;
				base.DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				lblError.Text = posOpenRegisterResponseDto.ErrorMessage ?? "Error desconocido.";
			}
		}
		catch (Exception)
		{
			lblError.Text = "Error de conexión con el servidor central.";
		}
		finally
		{
			btnOpen.Enabled = true;
		}
	}
}
