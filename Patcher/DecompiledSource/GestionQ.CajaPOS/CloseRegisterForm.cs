using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestionQ.CajaPOS;

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
		base.Size = new System.Drawing.Size(350, 300);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		System.Windows.Forms.Label value = new System.Windows.Forms.Label
		{
			Text = "Efectivo en Caja ($)",
			Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
			Dock = System.Windows.Forms.DockStyle.Top,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Height = 60
		};
		base.Controls.Add(value);
		this.txtFinalBalance = new System.Windows.Forms.TextBox
		{
			Font = new System.Drawing.Font("Segoe UI", 24f),
			TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
			Width = 200,
			Location = new System.Drawing.Point(65, 80)
		};
		this.txtFinalBalance.KeyPress += delegate(object? s, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
			{
				e.Handled = true;
			}
		};
		this.txtFinalBalance.KeyDown += delegate(object? s, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == System.Windows.Forms.Keys.Return)
			{
				this.btnClose.PerformClick();
			}
		};
		base.Controls.Add(this.txtFinalBalance);
		this.btnClose = new System.Windows.Forms.Button
		{
			Text = "Cerrar Caja",
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Width = 200,
			Height = 40,
			Location = new System.Drawing.Point(65, 140),
			BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnClose.Click += new System.EventHandler(BtnClose_Click);
		base.Controls.Add(this.btnClose);
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

	private async void BtnClose_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtFinalBalance.Text))
		{
			return;
		}
		if (!decimal.TryParse(txtFinalBalance.Text.Replace(".", ","), out var result))
		{
			lblError.Text = "Monto inválido.";
			return;
		}
		btnClose.Enabled = false;
		lblError.Text = "Cerrando caja...";
		try
		{
			if (await _authClient.CloseRegisterAsync(_cashRegisterId, result))
			{
				CloseSuccess = true;
				try
				{
					TicketText = await _authClient.GetRegisterTicketAsync(_cashRegisterId);
				}
				catch
				{
				}
				base.DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				lblError.Text = "Error al cerrar la caja en el servidor.";
			}
		}
		catch (Exception)
		{
			lblError.Text = "Error de conexión con el servidor central.";
		}
		finally
		{
			btnClose.Enabled = true;
		}
	}
}
