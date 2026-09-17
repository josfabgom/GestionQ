using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GestionQ.Domain.Entities;

namespace GestionQ.CajaPOS;

public class PartialReportForm : Form
{
	private int _cashRegisterId;

	private string _userName;

	private DateTime _openingDate;

	private decimal _initialBalance;

	private LocalDbContext _db;

	private Label lblTotalVentas;

	private Label lblEfectivoEsperado;

	private DataGridView gridMethods;

	private DataGridView gridProducts;

	private Button btnPrint;

	private Button btnClose;

	public PartialReportForm(int cashRegisterId, string userName, DateTime openingDate, decimal initialBalance)
	{
		_cashRegisterId = cashRegisterId;
		_userName = userName;
		_openingDate = openingDate;
		_initialBalance = initialBalance;
		_db = new LocalDbContext();
		InitializeComponent();
		LoadData();
	}

	private void InitializeComponent()
	{
		this.Text = "Reporte Parcial (Corte X)";
		base.Size = new System.Drawing.Size(800, 600);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
		System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel
		{
			Dock = System.Windows.Forms.DockStyle.Top,
			Height = 60,
			BackColor = System.Drawing.Color.FromArgb(16, 185, 129)
		};
		System.Windows.Forms.Label value = new System.Windows.Forms.Label
		{
			Text = $"CORTE PARCIAL - Caja #{this._cashRegisterId} ({this._userName})",
			Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
			ForeColor = System.Drawing.Color.White,
			Dock = System.Windows.Forms.DockStyle.Fill,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		};
		panel.Controls.Add(value);
		base.Controls.Add(panel);
		System.Windows.Forms.Panel panel2 = new System.Windows.Forms.Panel
		{
			Location = new System.Drawing.Point(20, 80),
			Size = new System.Drawing.Size(350, 420),
			BackColor = System.Drawing.Color.White,
			BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
		};
		System.Windows.Forms.Label value2 = new System.Windows.Forms.Label
		{
			Text = "Resumen de Caja",
			Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
			Location = new System.Drawing.Point(10, 10),
			AutoSize = true
		};
		this.lblTotalVentas = new System.Windows.Forms.Label
		{
			Font = new System.Drawing.Font("Segoe UI", 12f),
			Location = new System.Drawing.Point(10, 45),
			AutoSize = true
		};
		this.lblEfectivoEsperado = new System.Windows.Forms.Label
		{
			Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
			ForeColor = System.Drawing.Color.Green,
			Location = new System.Drawing.Point(10, 120),
			AutoSize = true
		};
		panel2.Controls.Add(value2);
		panel2.Controls.Add(this.lblTotalVentas);
		panel2.Controls.Add(this.lblEfectivoEsperado);
		System.Windows.Forms.Label value3 = new System.Windows.Forms.Label
		{
			Text = "Medios de Pago:",
			Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold),
			Location = new System.Drawing.Point(10, 155),
			AutoSize = true
		};
		panel2.Controls.Add(value3);
		this.gridMethods = new System.Windows.Forms.DataGridView
		{
			Location = new System.Drawing.Point(10, 185),
			Size = new System.Drawing.Size(330, 220),
			AllowUserToAddRows = false,
			ReadOnly = true,
			RowHeadersVisible = false,
			AllowUserToResizeColumns = false,
			AllowUserToResizeRows = false,
			BackgroundColor = System.Drawing.Color.White,
			AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill,
			SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
		};
		panel2.Controls.Add(this.gridMethods);
		base.Controls.Add(panel2);
		System.Windows.Forms.Panel panel3 = new System.Windows.Forms.Panel
		{
			Location = new System.Drawing.Point(390, 80),
			Size = new System.Drawing.Size(370, 420),
			BackColor = System.Drawing.Color.White,
			BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
		};
		System.Windows.Forms.Label value4 = new System.Windows.Forms.Label
		{
			Text = "Artículos Vendidos",
			Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold),
			Location = new System.Drawing.Point(10, 10),
			AutoSize = true
		};
		panel3.Controls.Add(value4);
		this.gridProducts = new System.Windows.Forms.DataGridView
		{
			Location = new System.Drawing.Point(10, 45),
			Size = new System.Drawing.Size(350, 360),
			AllowUserToAddRows = false,
			ReadOnly = true,
			RowHeadersVisible = false,
			AllowUserToResizeColumns = false,
			AllowUserToResizeRows = false,
			BackgroundColor = System.Drawing.Color.White,
			SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
		};
		panel3.Controls.Add(this.gridProducts);
		base.Controls.Add(panel3);
		this.btnPrint = new System.Windows.Forms.Button
		{
			Text = "\ud83d\udda8\ufe0f Imprimir Ticket",
			Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold),
			Location = new System.Drawing.Point(390, 510),
			Size = new System.Drawing.Size(180, 40),
			BackColor = System.Drawing.Color.FromArgb(0, 123, 255),
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnPrint.Click += new System.EventHandler(BtnPrint_Click);
		base.Controls.Add(this.btnPrint);
		this.btnClose = new System.Windows.Forms.Button
		{
			Text = "Cerrar",
			Font = new System.Drawing.Font("Segoe UI", 12f, System.Drawing.FontStyle.Bold),
			Location = new System.Drawing.Point(580, 510),
			Size = new System.Drawing.Size(180, 40),
			BackColor = System.Drawing.Color.Gray,
			ForeColor = System.Drawing.Color.White,
			FlatStyle = System.Windows.Forms.FlatStyle.Flat
		};
		this.btnClose.Click += delegate
		{
			base.Close();
		};
		base.Controls.Add(this.btnClose);
	}

	private void LoadData()
	{
		try
		{
			List<Sale> source = _db.Sales.Where((Sale s) => s.CashRegisterId == (int?)_cashRegisterId && s.Date >= _openingDate).ToList();
			List<int> ventaIds = source.Select((Sale v) => v.Id).ToList();
			List<SalePayment> source2 = _db.SalePayments.Where((SalePayment p) => ventaIds.Contains(p.SaleId)).ToList();
			List<SaleItem> source3 = _db.SaleItems.Where((SaleItem i) => ventaIds.Contains(i.SaleId)).ToList();
			List<CashRegisterMovement> source4 = _db.Movements.Where((CashRegisterMovement m) => m.CashRegisterId == _cashRegisterId && m.Date >= _openingDate).ToList();
			Dictionary<int, string> methodsDict = _db.PaymentMethods.ToDictionary((PaymentMethod m) => m.Id, (PaymentMethod m) => m.Name);
			Dictionary<int, string> productsDict = _db.Products.ToDictionary((Product p) => p.Id, (Product p) => p.Name);
			decimal value = source.Sum((Sale v) => v.TotalAmount);
			decimal num = source4.Where((CashRegisterMovement m) => m.Type == "IN").Sum((CashRegisterMovement m) => m.Amount);
			decimal num2 = source4.Where((CashRegisterMovement m) => m.Type == "OUT").Sum((CashRegisterMovement m) => m.Amount);
			decimal num3 = source2.Where((SalePayment p) => (methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Efectivo") == "Efectivo").Sum((SalePayment p) => p.Amount);
			decimal value2 = _initialBalance + num3 + num - num2;
			lblTotalVentas.Text = $"Fondo Inicial: {_initialBalance:C2}\nTotal Ventas: {value:C2}\nIngresos: {num:C2} | Retiros: {num2:C2}";
			lblEfectivoEsperado.Text = $"Efectivo Esperado: {value2:C2}";
			var dataSource = (from p in source2
				group p by (!methodsDict.ContainsKey(p.PaymentMethodId)) ? "Desconocido" : methodsDict[p.PaymentMethodId] into g
				select new
				{
					Metodo = g.Key,
					Total = g.Sum((SalePayment p) => p.Amount)
				}).ToList();
			gridMethods.DataSource = dataSource;
			if (gridMethods.Columns.Count > 0)
			{
				gridMethods.Columns["Total"].DefaultCellStyle.Format = "C2";
			}
			var dataSource2 = (from i in source3
				group i by (!productsDict.ContainsKey(i.ProductId)) ? ("Producto " + i.ProductId) : productsDict[i.ProductId] into g
				select new
				{
					Producto = g.Key,
					Cant = g.Sum((SaleItem i) => i.Quantity),
					Total = g.Sum((SaleItem i) => i.UnitPrice * i.Quantity)
				} into g
				orderby g.Cant descending
				select g).ToList();
			gridProducts.DataSource = dataSource2;
			if (gridProducts.Columns.Count > 0)
			{
				gridProducts.Columns["Producto"].Width = 180;
				gridProducts.Columns["Cant"].Width = 60;
				gridProducts.Columns["Total"].Width = 90;
				gridProducts.Columns["Total"].DefaultCellStyle.Format = "C2";
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al cargar datos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void BtnPrint_Click(object sender, EventArgs e)
	{
		try
		{
			List<Sale> source = _db.Sales.Where((Sale s) => s.CashRegisterId == (int?)_cashRegisterId && s.Date >= _openingDate && !s.IsCancelled).ToList();
			List<int> ventaIds = source.Select((Sale v) => v.Id).ToList();
			List<SalePayment> source2 = _db.SalePayments.Where((SalePayment p) => ventaIds.Contains(p.SaleId)).ToList();
			List<CashRegisterMovement> source3 = _db.Movements.Where((CashRegisterMovement m) => m.CashRegisterId == _cashRegisterId && m.Date >= _openingDate).ToList();
			Dictionary<int, string> methodsDict = _db.PaymentMethods.ToDictionary((PaymentMethod m) => m.Id, (PaymentMethod m) => m.Name);
			decimal value = source.Sum((Sale v) => v.TotalAmount);
			decimal num = source3.Where((CashRegisterMovement m) => m.Type == "IN").Sum((CashRegisterMovement m) => m.Amount);
			decimal num2 = source3.Where((CashRegisterMovement m) => m.Type == "OUT").Sum((CashRegisterMovement m) => m.Amount);
			decimal num3 = source2.Where((SalePayment p) => (methodsDict.ContainsKey(p.PaymentMethodId) ? methodsDict[p.PaymentMethodId] : "Efectivo") == "Efectivo").Sum((SalePayment p) => p.Amount);
			decimal value2 = _initialBalance + num3 + num - num2;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("================================");
			stringBuilder.AppendLine("         CORTE PARCIAL (X)      ");
			stringBuilder.AppendLine("================================");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
			handler.AppendLiteral("Fecha: ");
			handler.AppendFormatted(DateTime.Now);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
			handler.AppendLiteral("Caja: #");
			handler.AppendFormatted(_cashRegisterId);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
			handler.AppendLiteral("Cajero: ");
			handler.AppendFormatted(_userName);
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("Apertura: ");
			handler.AppendFormatted(_openingDate);
			stringBuilder6.AppendLine(ref handler);
			stringBuilder.AppendLine("--------------------------------");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("Fondo Inicial: ");
			handler.AppendFormatted(_initialBalance, "C2");
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("Ingresos Extra: ");
			handler.AppendFormatted(num, "C2");
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("Retiros: ");
			handler.AppendFormatted(num2, "C2");
			stringBuilder9.AppendLine(ref handler);
			stringBuilder.AppendLine("--------------------------------");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("TOTAL VENTAS: ");
			handler.AppendFormatted(value, "C2");
			stringBuilder10.AppendLine(ref handler);
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("MEDIOS DE PAGO:");
			foreach (var item in (from p in source2
				group p by (!methodsDict.ContainsKey(p.PaymentMethodId)) ? "Desconocido" : methodsDict[p.PaymentMethodId] into g
				select new
				{
					Metodo = g.Key,
					Total = g.Sum((SalePayment p) => p.Amount)
				}).ToList())
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder11 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
				handler.AppendLiteral("- ");
				handler.AppendFormatted(item.Metodo);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.Total, "C2");
				stringBuilder11.AppendLine(ref handler);
			}
			stringBuilder.AppendLine("--------------------------------");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
			handler.AppendLiteral("EFECTIVO ESPERADO: ");
			handler.AppendFormatted(value2, "C2");
			stringBuilder12.AppendLine(ref handler);
			stringBuilder.AppendLine("================================");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("");
			PrintTicket(stringBuilder.ToString());
			MessageBox.Show("Ticket de corte enviado a imprimir.", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al imprimir el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void PrintTicket(string text)
	{
		string text2 = text;
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		try
		{
			PrintDocument printDocument = new PrintDocument();
			string text3 = "";
			using (LocalDbContext localDbContext = new LocalDbContext())
			{
				SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "TicketPrinter");
				if (systemSetting != null)
				{
					text3 = systemSetting.Value;
				}
			}
			if (!string.IsNullOrEmpty(text3))
			{
				printDocument.PrinterSettings.PrinterName = text3;
			}
			printDocument.PrintPage += delegate(object s, PrintPageEventArgs ev)
			{
				Font font = new Font("Courier New", 8f, FontStyle.Bold);
				SolidBrush brush = new SolidBrush(Color.Black);
				float num = 10f;
				float x = 0f;
				if (ev.Graphics != null)
				{
					string[] array = text2.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
					foreach (string s2 in array)
					{
						ev.Graphics.DrawString(s2, font, brush, x, num);
						num += font.GetHeight(ev.Graphics);
					}
				}
			};
			printDocument.Print();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error de impresión: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_db?.Dispose();
		}
		base.Dispose(disposing);
	}
}
