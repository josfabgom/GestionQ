using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestionQ.CajaPOS;

public class MovementForm : Form
{
	private AuthClient _authClient;

	private int _cashRegisterId;

	private TextBox txtAmount;

	private TextBox txtDescription;

	private Button btnSave;

	private Label lblError;

	public bool SaveSuccess { get; private set; }

	public decimal Amount
	{
		get
		{
			if (!decimal.TryParse(txtAmount.Text.Replace(".", ","), out var result))
			{
				return 0m;
			}
			return result;
		}
	}

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
		base.Size = new System.Drawing.Size(400, 380);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		System.Windows.Forms.Label value = new System.Windows.Forms.Label
		{
			Text = "Nuevo Egreso",
			Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
			Dock = System.Windows.Forms.DockStyle.Top,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Height = 50
		};
		base.Controls.Add(value);
		System.Windows.Forms.Label value2 = new System.Windows.Forms.Label
		{
			Text = "Monto a Retirar ($):",
			Font = new System.Drawing.Font("Segoe UI", 10f),
			Location = new System.Drawing.Point(50, 70),
			Width = 300
		};
		base.Controls.Add(value2);
		this.txtAmount = new System.Windows.Forms.TextBox
		{
			Font = new System.Drawing.Font("Segoe UI", 20f),
			TextAlign = System.Windows.Forms.HorizontalAlignment.Center,
			Width = 280,
			Location = new System.Drawing.Point(50, 100)
		};
		this.txtAmount.KeyPress += delegate(object? s, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
			{
				e.Handled = true;
			}
		};
		this.txtAmount.KeyDown += delegate(object? s, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == System.Windows.Forms.Keys.Return)
			{
				this.txtDescription.Focus();
			}
		};
		base.Controls.Add(this.txtAmount);
		System.Windows.Forms.Label value3 = new System.Windows.Forms.Label
		{
			Text = "Motivo (Ej. Pago a Proveedor):",
			Font = new System.Drawing.Font("Segoe UI", 10f),
			Location = new System.Drawing.Point(50, 160),
			Width = 300
		};
		base.Controls.Add(value3);
		this.txtDescription = new System.Windows.Forms.TextBox
		{
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Width = 280,
			Location = new System.Drawing.Point(50, 190)
		};
		this.txtDescription.KeyDown += delegate(object? s, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == System.Windows.Forms.Keys.Return)
			{
				this.btnSave.PerformClick();
			}
		};
		base.Controls.Add(this.txtDescription);
		this.btnSave = new System.Windows.Forms.Button
		{
			Text = "Registrar Retiro",
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Width = 280,
			Height = 40,
			Location = new System.Drawing.Point(50, 240),
			BackColor = System.Drawing.Color.FromArgb(0, 123, 255),
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnSave.Click += new System.EventHandler(BtnSave_Click);
		base.Controls.Add(this.btnSave);
		this.lblError = new System.Windows.Forms.Label
		{
			ForeColor = System.Drawing.Color.Red,
			Font = new System.Drawing.Font("Segoe UI", 9f),
			Width = 380,
			Location = new System.Drawing.Point(0, 290),
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		};
		base.Controls.Add(this.lblError);
	}

	private async void BtnSave_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtAmount.Text) || string.IsNullOrWhiteSpace(txtDescription.Text))
		{
			lblError.Text = "Debe ingresar el monto y el motivo.";
			return;
		}
		if (!decimal.TryParse(txtAmount.Text.Replace(".", ","), out var result) || result <= 0m)
		{
			lblError.Text = "Monto inválido.";
			return;
		}
		btnSave.Enabled = false;
		lblError.Text = "Registrando movimiento...";
		try
		{
			var (flag, text) = await _authClient.AddMovementAsync(_cashRegisterId, result, txtDescription.Text, "Egreso");
			if (flag)
			{
				SaveSuccess = true;
				base.DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				lblError.Text = text;
			}
		}
		catch (Exception)
		{
			lblError.Text = "Error de red con el servidor central.";
		}
		finally
		{
			btnSave.Enabled = true;
		}
	}
}
