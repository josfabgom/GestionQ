using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestionQ.Domain.Entities;

namespace GestionQ.CajaPOS;

public class SalesHistoryForm : Form
{
	private DataGridView gridSales;

	private Button btnCancelSale;

	private Button btnClose;

	private int _cashRegisterId;

	private int _posNumber;

	public SalesHistoryForm(int cashRegisterId, int posNumber)
	{
		_cashRegisterId = cashRegisterId;
		_posNumber = posNumber;
		InitializeUI();
		LoadSales();
	}

	private void InitializeUI()
	{
		Text = "Historial de Ventas (Turno Actual)";
		base.Size = new Size(800, 500);
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		gridSales = new DataGridView
		{
			Location = new Point(10, 10),
			Size = new Size(760, 390),
			AllowUserToAddRows = false,
			ReadOnly = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			MultiSelect = false,
			BackgroundColor = Color.White,
			RowHeadersVisible = false,
			AllowUserToResizeRows = false
		};
		gridSales.Columns.Add("Id", "ID Local");
		gridSales.Columns.Add("Ticket", "Comprobante");
		gridSales.Columns.Add("Date", "Fecha / Hora");
		gridSales.Columns.Add("Total", "Total");
		gridSales.Columns.Add("Status", "Estado");
		gridSales.Columns["Id"].Visible = false;
		gridSales.Columns["Ticket"].Width = 150;
		gridSales.Columns["Date"].Width = 150;
		gridSales.Columns["Total"].Width = 100;
		gridSales.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		btnCancelSale = new Button
		{
			Text = "Anular Venta Seleccionada",
			Location = new Point(10, 410),
			Size = new Size(200, 40),
			BackColor = Color.FromArgb(239, 68, 68),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold)
		};
		btnCancelSale.Click += BtnCancelSale_Click;
		btnClose = new Button
		{
			Text = "Cerrar",
			Location = new Point(670, 410),
			Size = new Size(100, 40),
			BackColor = Color.Gray,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold)
		};
		btnClose.Click += delegate
		{
			Close();
		};
		base.Controls.Add(gridSales);
		base.Controls.Add(btnCancelSale);
		base.Controls.Add(btnClose);
	}

	private void LoadSales()
	{
		gridSales.Rows.Clear();
		using LocalDbContext localDbContext = new LocalDbContext();
		foreach (Sale item in (from s in localDbContext.Sales
			where s.CashRegisterId == (int?)_cashRegisterId
			orderby s.Id descending
			select s).ToList())
		{
			string text = $"{_posNumber:D5}-{item.Id:D8}";
			string text2 = (item.IsCancelled ? $"Anulada el {item.CancellationDate:dd/MM/yyyy HH:mm}" : "Completada");
			int index = gridSales.Rows.Add(item.Id, text, item.Date.ToString("dd/MM/yyyy HH:mm"), item.TotalAmount.ToString("C2"), text2);
			if (item.IsCancelled)
			{
				gridSales.Rows[index].DefaultCellStyle.ForeColor = Color.Red;
				gridSales.Rows[index].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Strikeout);
			}
		}
	}

	private void BtnCancelSale_Click(object sender, EventArgs e)
	{
		if (gridSales.SelectedRows.Count == 0)
		{
			return;
		}
		int saleId = (int)gridSales.SelectedRows[0].Cells["Id"].Value;
		using LocalDbContext localDbContext = new LocalDbContext();
		Sale sale = localDbContext.Sales.FirstOrDefault((Sale s) => s.Id == saleId);
		if (sale == null || sale.IsCancelled)
		{
			MessageBox.Show("Esta venta ya está anulada o no existe.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		else if (MessageBox.Show($"¿Estás seguro de que deseas anular el comprobante {_posNumber:D5}-{sale.Id:D8} por {sale.TotalAmount:C2}?\n\nEsta acción es irreversible y se descontará del cierre de caja actual.", "Confirmar Anulación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
		{
			sale.IsCancelled = true;
			sale.CancellationDate = DateTime.Now;
			sale.IsSynced = false;
			localDbContext.SaveChanges();
			MessageBox.Show("Venta anulada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			LoadSales();
		}
	}
}
