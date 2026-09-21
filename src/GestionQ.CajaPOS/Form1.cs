using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace GestionQ.CajaPOS;

public class Form1 : Form
{
	private class ComboBoxItem
	{
		public string Text { get; set; }

		public int Value { get; set; }
		public int? PresentationId { get; set; }

		public ComboBoxItem(string text, int value, int? presentationId = null)
		{
			Text = text;
			Value = value;
			PresentationId = presentationId;
		}

		public override string ToString()
		{
			return Text;
		}
	}

	private Color bgColor = Color.FromArgb(17, 24, 39);

	private Color panelColor = Color.FromArgb(31, 41, 55);

	private Color accentColor = Color.FromArgb(139, 92, 246);

	private Color greenColor = Color.FromArgb(16, 185, 129);

	private Color textColor = Color.White;

	private DataGridView gridItems = new DataGridView();

	private Label lblTotal = new Label();

	private Label lblItemsCount = new Label();

	private ComboBox cmbCustomer = new ComboBox();

	private ComboBox cmbPaymentMethod = new ComboBox();

	private TextBox txtBarcode = new TextBox();

	private Dictionary<Keys, Department> _departmentHotkeys = new Dictionary<Keys, Department>();

	private ListBox lstSearch = new ListBox();

	private FlowLayoutPanel panelDepartments = new FlowLayoutPanel();

	private Label lblVuelto = new Label();

	private Button btnSettings = new Button();

	private Label lblSubTotalValue = new Label();

	private Label lblSubTotalText = new Label();

	private Label lblTotalText = new Label();

	private Label lblPromoDiscountValue = new Label();

	private Label lblPromoDiscountText = new Label();

	private Button btnSync = new Button();

	private Label lblMultiplier = new Label();

	private PictureBox picArticle = new PictureBox();

	private decimal _nextQuantity = 1m;

	private Label lblPromoStatus = new Label();

	private List<PromotionSyncDto> _activePromotions = new List<PromotionSyncDto>();
	private List<ProductPresentation> _activePresentations = new List<ProductPresentation>();

	private SyncWorker _syncWorker;

	private AuthClient _authClient;

	private string _userId;

	private string _userName;

	private int _posNumber;

	private int _cashRegisterId;

	private string _posIdentifier = Environment.MachineName;

	private Button btnCloseRegister = new Button();

	private Button btnAddMovement = new Button();

	private Button btnPartialReport = new Button();

	private Button btnSalesHistory = new Button();

	private bool _requestElectronicInvoice;

	private Label lblTitle = new Label();

	private void ToggleInvoiceType()
	{
		_requestElectronicInvoice = !_requestElectronicInvoice;
		if (_requestElectronicInvoice)
		{
			lblTitle.ForeColor = Color.DeepSkyBlue;
			lblTitle.Text = "\ud83d\udce0 Punto de Venta (Caja)   ●";
		}
		else
		{
			lblTitle.ForeColor = Color.WhiteSmoke;
			lblTitle.Text = "\ud83d\udce0 Punto de Venta (Caja)";
		}
	}

	public Form1()
	{
		_authClient = new AuthClient(AppConfig.ServerUrl);
		InitializeUI();
		_syncWorker = new SyncWorker(AppConfig.ServerUrl);
		_syncWorker.OnSyncCompleted += delegate
		{
			Invoke((MethodInvoker)delegate
			{
				LoadInitialDataAsync();
				btnSync.Text = "Sincronizado ✔\ufe0f";
				btnSync.ForeColor = greenColor;
			});
		};
		_syncWorker.OnSyncError += delegate
		{
			Invoke((MethodInvoker)delegate
			{
				btnSync.Text = "⚠ OFFLINE (Local)";
				btnSync.ForeColor = Color.Red;
			});
		};
		base.Shown += Form1_Shown;
	}

	private async void Form1_Shown(object? sender, EventArgs e)
	{
		Hide();
		LoginForm loginForm = new LoginForm(_authClient, _posIdentifier);
		if (loginForm.ShowDialog(this) != DialogResult.OK)
		{
			Application.Exit();
			return;
		}
		_userId = loginForm.LoginResult.UserId;
		try
		{
			using (LocalDbContext localDbContext = new LocalDbContext())
			{
				SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosNumber");
				if (systemSetting != null && int.TryParse(systemSetting.Value, out int posNum))
				{
					_posNumber = posNum;
				}
				else
				{
				    _posNumber = 1;
				}
				
				var openRegister = localDbContext.OfflineCashRegisters.FirstOrDefault(c => c.ClosingDate == null);
				
				if (openRegister != null)
				{
					if (openRegister.UserId != _userId)
					{
						MessageBox.Show("La caja actual fue abierta por otro usuario. Para operar, inicie sesión con su usuario, o cierre la caja.", "Caja Ocupada", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						Application.Exit();
						return;
					}
					_cashRegisterId = openRegister.Id;
				}
				else
				{
					OpenRegisterForm openRegisterForm = new OpenRegisterForm(_authClient, _userId, _posIdentifier);
					if (openRegisterForm.ShowDialog(this) != DialogResult.OK)
					{
						Application.Exit();
						return;
					}
					_cashRegisterId = openRegisterForm.OpenResult.CashRegisterId.Value;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al verificar estado de caja con el servidor. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			Application.Exit();
			return;
		}
		Show();
		string value = (_userName = ((!string.IsNullOrEmpty(loginForm.LoginResult.FullName)) ? loginForm.LoginResult.FullName : loginForm.LoginResult.UserName));
		Text = $"GestionQ - Punto de Venta (Caja) - Cajero: {value} - Caja: {_cashRegisterId}";
		_syncWorker.Start();
		LoadInitialDataAsync();
	}

	private async void LoadInitialDataAsync()
	{
		using LocalDbContext db = new LocalDbContext();
		db.Database.EnsureCreated();
		try
		{
			db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS SystemSettings (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Key\" TEXT NOT NULL, Value TEXT NULL, Description TEXT NULL);");
		}
		catch
		{
		}
		try
		{
			db.Database.ExecuteSqlRaw("ALTER TABLE ProductPresentations ADD COLUMN IsBulk INTEGER NOT NULL DEFAULT 0;");
		}
		catch
		{
		}
		try
		{
			db.Database.ExecuteSqlRaw("ALTER TABLE Sales ADD COLUMN RequestElectronicInvoice INTEGER NOT NULL DEFAULT 0;");
		}
		catch
		{
		}
		try
		{
			db.Database.ExecuteSqlRaw("ALTER TABLE PaymentMethods ADD COLUMN DiscountValidFrom TEXT;");
		}
		catch
		{
		}
		try
		{
			db.Database.ExecuteSqlRaw("ALTER TABLE PaymentMethods ADD COLUMN DiscountValidTo TEXT;");
		}
		catch
		{
		}
		List<Customer> list = await db.Customers.ToListAsync();
		list.Insert(0, new Customer
		{
			Id = 0,
			Name = "Consumidor Final"
		});
		object selectedValue = cmbCustomer.SelectedValue;
		cmbCustomer.DataSource = list;
		cmbCustomer.DisplayMember = "Name";
		cmbCustomer.ValueMember = "Id";
		if (selectedValue != null)
		{
			cmbCustomer.SelectedValue = selectedValue;
		}
		List<PaymentMethod> list2 = await db.PaymentMethods.Where((PaymentMethod p) => p.IsActive).ToListAsync();
		if (list2.Count == 0)
		{
			list2.Add(new PaymentMethod
			{
				Id = 1,
				Name = "Efectivo"
			});
		}
		object selectedValue2 = cmbPaymentMethod.SelectedValue;
		cmbPaymentMethod.DataSource = list2;
		cmbPaymentMethod.DisplayMember = "Name";
		cmbPaymentMethod.ValueMember = "Id";
		if (selectedValue2 != null)
		{
			cmbPaymentMethod.SelectedValue = selectedValue2;
		}

		try
		{
			_activePresentations = await db.ProductPresentations.Where(p => p.IsActive).ToListAsync();
		}
		catch
		{
			_activePresentations = new List<ProductPresentation>();
		}

		SystemSetting systemSetting = await db.SystemSettings.FirstOrDefaultAsync((SystemSetting s) => s.Key == "ActivePromotions");
		if (systemSetting != null && !string.IsNullOrEmpty(systemSetting.Value))
		{
			try
			{
				_activePromotions = JsonSerializer.Deserialize<List<PromotionSyncDto>>(systemSetting.Value) ?? new List<PromotionSyncDto>();
			}
			catch
			{
				_activePromotions = new List<PromotionSyncDto>();
			}
		}
		if (_activePromotions.Any())
		{
			lblPromoStatus.Text = string.Join("\n", _activePromotions.Select((PromotionSyncDto p) => "• " + p.Name));
			lblPromoStatus.ForeColor = Color.FromArgb(16, 185, 129);
		}
		else
		{
			lblPromoStatus.Text = "No hay promociones activas.";
			lblPromoStatus.ForeColor = Color.Gray;
		}
		List<Department> list3 = await db.Departments.ToListAsync();
		panelDepartments.Controls.Clear();
		_departmentHotkeys.Clear();
		foreach (Department dept in list3)
		{
			if (!string.IsNullOrEmpty(dept.Hotkey) && Enum.TryParse<Keys>(dept.Hotkey, ignoreCase: true, out var result))
			{
				_departmentHotkeys[result] = dept;
			}
			Button button = new Button
			{
				Text = (string.IsNullOrEmpty(dept.Hotkey) ? dept.Name : (dept.Name + "\n[" + dept.Hotkey + "]")),
				Width = 80,
				Height = 45,
				FlatStyle = FlatStyle.Flat,
				ForeColor = textColor,
				BackColor = bgColor,
				Font = new Font("Segoe UI", 9f)
			};
			button.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
			button.Tag = dept;
			button.Click += async delegate
			{
				await ProcessDepartmentSale(dept);
			};
			panelDepartments.Controls.Add(button);
		}
		if (list3.Count == 0)
		{
			Label value = new Label
			{
				Text = "No hay departamentos",
				ForeColor = Color.Gray,
				AutoSize = true
			};
			panelDepartments.Controls.Add(value);
		}
		
		txtBarcode.Focus();
	}

	private void InitializeUI()
	{
		Text = "GestionQ - Punto de Venta (Caja)";
		base.Size = new Size(1366, 768);
		base.WindowState = FormWindowState.Maximized;
		BackColor = bgColor;
		ForeColor = textColor;
		Font = new Font("Segoe UI", 10f);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 2,
			Padding = new Padding(20, 20, 20, 0)
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 0, 20, 0)
		};
		Panel panel2 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 50
		};
		lblTitle.Text = "\ud83d\udce0 Punto de Venta (Caja)";
		lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
		lblTitle.AutoSize = true;
		lblTitle.Location = new Point(0, 10);
		lblTitle.ForeColor = Color.WhiteSmoke;
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Right,
			FlowDirection = FlowDirection.RightToLeft,
			AutoSize = true,
			WrapContents = false,
			Padding = new Padding(0, 10, 0, 0)
		};
		btnSettings = new Button
		{
			Text = "⚙\ufe0f Configurar",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = textColor,
			Margin = new Padding(10, 0, 0, 0)
		};
		btnSettings.FlatAppearance.BorderSize = 0;
		btnSettings.Click += delegate
		{
			string serverUrl = AppConfig.ServerUrl;
			string text4 = Interaction.InputBox("Ingresa la IP o URL del Servidor Principal (ej: http://192.168.1.50:5144):", "Configuración de Servidor", serverUrl);
			if (!string.IsNullOrWhiteSpace(text4) && text4 != serverUrl)
			{
				AppConfig.ServerUrl = text4;
				MessageBox.Show("Configuración guardada. La caja se reiniciará para aplicar los cambios.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				Application.Restart();
				Environment.Exit(0);
			}
		};
		btnSync = new Button
		{
			Text = "\ud83d\udd04 Sincronizar",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = textColor,
			Margin = new Padding(10, 0, 0, 0)
		};
		btnSync.FlatAppearance.BorderSize = 0;
		btnSync.Click += async delegate
		{
			btnSync.Text = "⏳ Sincronizando...";
			btnSync.ForeColor = Color.Yellow;
			try
			{
				await _syncWorker.PerformSyncAsync();
			}
			catch (Exception ex2)
			{
				btnSync.Text = "⚠ OFFLINE (Local)";
				btnSync.ForeColor = Color.Red;
				/* MessageBox.Show bloqueante removido para UX Offline */
			}
		};
		btnCloseRegister = new Button
		{
			Text = "\ud83d\udd12 Cerrar Caja",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.FromArgb(239, 68, 68),
			Margin = new Padding(10, 0, 0, 0)
		};
		btnCloseRegister.FlatAppearance.BorderSize = 0;
		btnCloseRegister.Click += async delegate
		{
			CloseRegisterForm closeRegisterForm = new CloseRegisterForm(_authClient, _cashRegisterId);
			if (closeRegisterForm.ShowDialog(this) == DialogResult.OK)
			{
				if (!string.IsNullOrEmpty(closeRegisterForm.TicketText))
				{
					PrintTicket(closeRegisterForm.TicketText);
				}
				btnSync.ForeColor = Color.Yellow;
				try
				{
					await _syncWorker.PerformSyncAsync();
				}
				catch
				{
				}
				MessageBox.Show("Caja cerrada exitosamente. La aplicación se reiniciará para el próximo cajero.", "Cierre", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				Application.Restart();
			}
		};
		btnAddMovement = new Button
		{
			Text = "\ud83d\udcb8 Retiro Efectivo",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.FromArgb(0, 123, 255),
			Margin = new Padding(10, 0, 0, 0)
		};
		btnAddMovement.FlatAppearance.BorderSize = 0;
		btnAddMovement.Click += delegate
		{
			MovementForm movementForm = new MovementForm(_authClient, _cashRegisterId);
			if (movementForm.ShowDialog(this) == DialogResult.OK)
			{
				string text3 = "      COMPROBANTE DE RETIRO\n";
				text3 = text3 + new string('-', 42) + "\n";
				text3 += $"Monto: $ {movementForm.Amount:N2}\n";
				text3 = text3 + "Motivo: " + movementForm.Description + "\n";
				text3 += $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n";
				text3 = text3 + new string('-', 42) + "\n\n\n\n";
				text3 += "Firma: ___________________________\n\n";
				text3 += "Aclaracion: ______________________\n\n";
				PrintTicket(text3);
				MessageBox.Show("Retiro registrado exitosamente.", "Egreso", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		};
		btnPartialReport = new Button
		{
			Text = "\ud83d\udcca Reporte Parcial",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.FromArgb(245, 158, 11),
			Margin = new Padding(10, 0, 0, 0)
		};
		btnPartialReport.FlatAppearance.BorderSize = 0;
		btnPartialReport.Click += delegate
		{
			try
			{
				decimal initialBalance = default(decimal);
				DateTime openingDate = DateTime.Today;
				using (LocalDbContext localDbContext = new LocalDbContext())
				{
					OfflineCashRegister offlineCashRegister = localDbContext.OfflineCashRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == _cashRegisterId || r.ServerCashRegisterId == (int?)_cashRegisterId);
					if (offlineCashRegister != null)
					{
						initialBalance = offlineCashRegister.InitialBalance;
						openingDate = offlineCashRegister.OpeningDate;
					}
				}
				new PartialReportForm(_cashRegisterId, _userName, openingDate, initialBalance).ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show("No se pudo abrir el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		};
		btnSalesHistory = new Button
		{
			Text = "\ud83d\udcdc Historial / Anular",
			AutoSize = true,
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.FromArgb(16, 185, 129),
			Margin = new Padding(10, 0, 0, 0)
		};
		btnSalesHistory.FlatAppearance.BorderSize = 0;
		btnSalesHistory.Click += delegate
		{
			DateTime openingDate = DateTime.Today;
			using (LocalDbContext localDbContext = new LocalDbContext())
			{
				OfflineCashRegister offlineCashRegister = localDbContext.OfflineCashRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == _cashRegisterId || r.ServerCashRegisterId == (int?)_cashRegisterId);
				if (offlineCashRegister != null)
				{
					openingDate = offlineCashRegister.OpeningDate;
				}
			}
			new SalesHistoryForm(_cashRegisterId, _posNumber, openingDate).ShowDialog(this);
		};
		flowLayoutPanel.Controls.Add(btnSync);
		flowLayoutPanel.Controls.Add(btnSettings);
		flowLayoutPanel.Controls.Add(btnCloseRegister);
		flowLayoutPanel.Controls.Add(btnPartialReport);
		flowLayoutPanel.Controls.Add(btnSalesHistory);
		flowLayoutPanel.Controls.Add(btnAddMovement);
		panel2.Controls.Add(lblTitle);
		
		string appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
		Label lblVersion = new Label
		{
			Text = "v" + appVersion,
			ForeColor = Color.Gray,
			Font = new Font("Segoe UI", 10f),
			AutoSize = true,
			Location = new Point(lblTitle.Right + 10, 18)
		};
		lblTitle.SizeChanged += (s, e) => {
			lblVersion.Location = new Point(lblTitle.Right + 10, 18);
		};
		panel2.Controls.Add(lblVersion);
		
		gridItems = new DataGridView
		{
			Dock = DockStyle.Fill,
			BackgroundColor = Color.FromArgb(20, 21, 30),
			BorderStyle = BorderStyle.None,
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
			GridColor = Color.FromArgb(40, 42, 54),
			EnableHeadersVisualStyles = false,
			AllowUserToAddRows = false,
			ReadOnly = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			RowHeadersVisible = false,
			RowTemplate = 
			{
				Height = 80
			}
		};
		gridItems.DefaultCellStyle.BackColor = panelColor;
		gridItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(28, 30, 42);
		gridItems.DefaultCellStyle.ForeColor = textColor;
		gridItems.DefaultCellStyle.Font = new Font("Segoe UI", 12f);
		gridItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 48, 66);
		gridItems.DefaultCellStyle.SelectionForeColor = textColor;
		gridItems.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
		gridItems.ColumnHeadersDefaultCellStyle.BackColor = bgColor;
		gridItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gray;
		gridItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
		gridItems.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
		gridItems.ColumnHeadersHeight = 45;
		gridItems.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
		gridItems.Columns.Add("Id", "ID");
		gridItems.Columns.Add("OriginalName", "OriginalName");
		gridItems.Columns.Add("Name", "PRODUCTO");
		gridItems.Columns["OriginalName"].Visible = false;
		gridItems.Columns.Add("Price", "PRECIO UNIT.");
		DataGridViewButtonColumn dataGridViewButtonColumn = new DataGridViewButtonColumn
		{
			Name = "btnMinus",
			HeaderText = "",
			Text = "-",
			UseColumnTextForButtonValue = true,
			Width = 45,
			FlatStyle = FlatStyle.Flat
		};
		dataGridViewButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66);
		dataGridViewButtonColumn.DefaultCellStyle.ForeColor = Color.White;
		gridItems.Columns.Add(dataGridViewButtonColumn);
		gridItems.Columns.Add("Quantity", "CANTIDAD");
		DataGridViewButtonColumn dataGridViewButtonColumn2 = new DataGridViewButtonColumn
		{
			Name = "btnPlus",
			HeaderText = "",
			Text = "+",
			UseColumnTextForButtonValue = true,
			Width = 45,
			FlatStyle = FlatStyle.Flat
		};
		dataGridViewButtonColumn2.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66);
		dataGridViewButtonColumn2.DefaultCellStyle.ForeColor = Color.White;
		gridItems.Columns.Add(dataGridViewButtonColumn2);
		gridItems.Columns.Add("Discount", "DESCUENTO");
		gridItems.Columns.Add("SubTotal", "SUBTOTAL");
		gridItems.Columns["Id"].Visible = false;
		gridItems.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		gridItems.Columns["Price"].Width = 140;
		gridItems.Columns["Quantity"].Width = 100;
		gridItems.Columns["Discount"].Width = 120;
		gridItems.Columns["SubTotal"].Width = 150;
		gridItems.Columns["Price"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
		gridItems.Columns["Quantity"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
		gridItems.Columns["Discount"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
		gridItems.Columns["SubTotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
		DataGridViewCellStyle defaultCellStyle = new DataGridViewCellStyle
		{
			Alignment = DataGridViewContentAlignment.MiddleRight,
			Format = "C2",
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			ForeColor = Color.White
		};
		gridItems.Columns["Price"].DefaultCellStyle = defaultCellStyle;
		gridItems.Columns["Discount"].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(248, 113, 113) };
		gridItems.Columns["SubTotal"].DefaultCellStyle = defaultCellStyle;
		gridItems.Columns["Name"].DefaultCellStyle = new DataGridViewCellStyle
		{
			Font = new Font("Segoe UI", 14f, FontStyle.Bold),
			WrapMode = DataGridViewTriState.True,
			Padding = new Padding(5, 10, 5, 10)
		};
		gridItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
		gridItems.Columns["Quantity"].DefaultCellStyle = new DataGridViewCellStyle
		{
			Alignment = DataGridViewContentAlignment.MiddleCenter,
			Font = new Font("Segoe UI", 14f, FontStyle.Bold),
			Format = "0.##"
		};
		gridItems.CellContentClick += GridItems_CellContentClick;
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(1),
			BackColor = bgColor
		};
		gridItems.Margin = new Padding(1);
		panel3.Controls.Add(gridItems);
		panel.Controls.Add(panel3);
		panel.Controls.Add(panel2);
		Panel rightPanel = new Panel
		{
			Dock = DockStyle.Fill
		};
		Panel panel4 = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 170,
			Padding = new Padding(0, 10, 0, 0)
		};
		Panel totalBox = new Panel
		{
			Height = 160,
			Dock = DockStyle.Top,
			BackColor = Color.FromArgb(10, 10, 15),
			Margin = new Padding(0, 0, 0, 10)
		};
		totalBox.Resize += (s, e) => totalBox.Invalidate();
		totalBox.Paint += delegate(object? s, PaintEventArgs e)
		{
			int thickness = 2;
			using (Pen pen = new Pen(greenColor, thickness))
			{
				int half = thickness / 2;
				e.Graphics.DrawRectangle(pen, half, half, totalBox.Width - thickness, totalBox.Height - thickness);
			}
		};
		lblSubTotalText = new Label
		{
			Text = "SubTotal:",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 150, 10),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblSubTotalValue = new Label
		{
			Text = "$0.00",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 10),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblPromoDiscountText = new Label
		{
			Text = "Descuento:",
			ForeColor = Color.Orange,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 150, 35),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right),
			Visible = false
		};
		lblPromoDiscountValue = new Label
		{
			Text = "-$0.00",
			ForeColor = Color.Orange,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 35),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right),
			Visible = false
		};
		lblTotalText = new Label
		{
			Text = "TOTAL A COBRAR:",
			ForeColor = greenColor,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 250, 70),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblTotal = new Label
		{
			Text = "$0.00",
			ForeColor = greenColor,
			Font = new Font("Segoe UI", 36f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 160, 55),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblItemsCount = new Label
		{
			Text = "Cantidad de Artículos: 0",
			ForeColor = Color.Magenta,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(10, totalBox.Height - 30),
			Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
		};
		totalBox.Controls.Add(lblSubTotalText);
		totalBox.Controls.Add(lblSubTotalValue);
		totalBox.Controls.Add(lblPromoDiscountText);
		totalBox.Controls.Add(lblPromoDiscountValue);
		totalBox.Controls.Add(lblTotalText);
		totalBox.Controls.Add(lblTotal);
		totalBox.Controls.Add(lblItemsCount);
		panel4.Controls.Add(totalBox);
		FlowLayoutPanel controlsPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(0, 10, 0, 0)
		};
		PictureBox picLogo = new PictureBox
		{
			Height = 100,
			Width = rightPanel.Width - 20,
			SizeMode = PictureBoxSizeMode.Zoom,
			Margin = new Padding(0, 0, 0, 10)
		};
		string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "logo.png");
		if (File.Exists(text))
		{
			picLogo.Image = Image.FromFile(text);
		}
		else
		{
			picLogo.Paint += delegate(object? s, PaintEventArgs e)
			{
				e.Graphics.DrawString("LOGO EMPRESA", new Font("Segoe UI", 20f, FontStyle.Bold), Brushes.LightGray, new PointF(10f, 30f));
			};
		}
		controlsPanel.Controls.Add(picLogo);
		GroupBox gbCliente = CreateGroupBox("Cliente (F5 para crear)", 80);
		cmbCustomer = new ComboBox
		{
			Width = gbCliente.Width - 110,
			Location = new Point(10, 30),
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = panelColor,
			ForeColor = textColor,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 12f)
		};
		Button btnNewCustomer = new Button
		{
			Text = "Nuevo (F5)",
			Width = 90,
			Height = 30,
			Location = new Point(cmbCustomer.Right + 5, 29),
			BackColor = Color.FromArgb(16, 185, 129),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold)
		};
		btnNewCustomer.FlatAppearance.BorderSize = 0;
		btnNewCustomer.Click += async delegate
		{
			await CreateNewCustomerDialog();
		};
		gbCliente.Controls.Add(cmbCustomer);
		gbCliente.Controls.Add(btnNewCustomer);
		controlsPanel.Controls.Add(gbCliente);
		GroupBox groupBox = CreateGroupBox("Promociones Activas", 80);
		lblPromoStatus = new Label
		{
			Text = "No hay promociones activas.",
			Font = new Font("Segoe UI", 10f, FontStyle.Italic),
			ForeColor = Color.Gray,
			AutoSize = true,
			Location = new Point(10, 40)
		};
		groupBox.Controls.Add(lblPromoStatus);
		controlsPanel.Controls.Add(groupBox);
		
		Panel splitPanel = new Panel
		{
			Height = 400,
			Margin = new Padding(0)
		};
		Panel leftSplit = new Panel
		{
			Dock = DockStyle.Left
		};
		Panel rightSplit = new Panel
		{
			Dock = DockStyle.Right
		};
		GroupBox gbScan = CreateGroupBox("Escanear o Buscar (F1)", 150);
		txtBarcode = new TextBox
		{
			Width = gbScan.Width - 90,
			Location = new Point(10, 30),
			BackColor = panelColor,
			ForeColor = textColor,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 16f),
			PlaceholderText = " ⏸ Escanee código o busque por nombre..."
		};
		lblMultiplier = new Label
		{
			Text = "x1",
			Visible = false,
			Width = 70,
			Height = 35,
			Location = new Point(txtBarcode.Right + 5, 30),
			BackColor = accentColor,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 14f, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleCenter
		};
		txtBarcode.KeyDown += TxtBarcode_KeyDown;
		txtBarcode.TextChanged += TxtBarcode_TextChanged;
		lstSearch = new ListBox
		{
			Width = gbScan.Width - 20,
			Height = 80,
			Location = new Point(10, 75),
			BackColor = panelColor,
			ForeColor = textColor,
			BorderStyle = BorderStyle.FixedSingle,
			Visible = false,
			Font = new Font("Segoe UI", 12f),
			DrawMode = DrawMode.OwnerDrawFixed,
			ItemHeight = 30
		};
		lstSearch.DrawItem += LstSearch_DrawItem;
		lstSearch.KeyDown += LstSearch_KeyDown;
		lstSearch.DoubleClick += LstSearch_DoubleClick;
		lstSearch.SelectedIndexChanged += async delegate
		{
			if (lstSearch.SelectedItem is ComboBoxItem comboBoxItem)
			{
				using LocalDbContext db = new LocalDbContext();
				Product product = await db.Products.FindAsync(comboBoxItem.Value);
				UpdateArticleImage(product?.ImageUrl);
			}
		};
		gbScan.Controls.Add(txtBarcode);
		gbScan.Controls.Add(lblMultiplier);
		controlsPanel.Controls.Add(gbScan);
		
		GroupBox gbImagen = CreateGroupBox("Imagen del Artículo", 230);
		gbImagen.Location = new Point(0, 0);
		picArticle = new PictureBox
		{
			Location = new Point(10, 30),
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = Color.FromArgb(20, 20, 30)
		};
		string text2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "logo.png");
		if (File.Exists(text2))
		{
			picArticle.Image = Image.FromFile(text2);
		}
		gbImagen.Controls.Add(picArticle);
		leftSplit.Controls.Add(gbImagen);
		
		GroupBox gbDepts = CreateGroupBox("Venta Rápida por Departamento", 390);
		panelDepartments = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(5, 20, 5, 5),
			AutoScroll = true
		};
		gbDepts.Controls.Add(panelDepartments);
		rightSplit.Controls.Add(gbDepts);
		splitPanel.Controls.Add(leftSplit);
		splitPanel.Controls.Add(rightSplit);
		controlsPanel.Controls.Add(splitPanel);
		Button button = new Button
		{
			Text = "✔\ufe0f FINALIZAR VENTA [F12]",
			Height = 60,
			Width = rightPanel.Width,
			Dock = DockStyle.Bottom,
			BackColor = accentColor,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 14f, FontStyle.Bold),
			FlatStyle = FlatStyle.Flat
		};
		button.FlatAppearance.BorderSize = 0;
		button.Click += BtnFinalize_Click;
		rightPanel.Controls.Add(controlsPanel);
		rightPanel.Controls.Add(button);
		rightPanel.Controls.Add(lstSearch);
		
		panel.Controls.Add(panel4);
		tableLayoutPanel.Controls.Add(panel, 0, 0);
		tableLayoutPanel.Controls.Add(rightPanel, 1, 0);

		Panel bottomMenu = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0),
			BackColor = Color.FromArgb(15, 15, 20)
		};
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.Padding = new Padding(0, 15, 20, 0);
		bottomMenu.Controls.Add(flowLayoutPanel);
		
		tableLayoutPanel.Controls.Add(bottomMenu, 0, 1);
		tableLayoutPanel.SetColumnSpan(bottomMenu, 2);
		base.Controls.Add(tableLayoutPanel);
		base.Resize += delegate
		{
			foreach (Control control in controlsPanel.Controls)
			{
				control.Width = rightPanel.Width - 20;
			}
			if (splitPanel != null)
			{
				splitPanel.Height = controlsPanel.Height - splitPanel.Top - 10;
				leftSplit.Width = (int)((double)splitPanel.Width * 0.4);
				rightSplit.Width = splitPanel.Width - leftSplit.Width;
				gbImagen.Width = leftSplit.Width - 10;
				gbImagen.Height = splitPanel.Height;
				gbDepts.Height = splitPanel.Height;
				gbDepts.Width = rightSplit.Width - 10;
				picArticle.Width = gbImagen.Width - 20;
				picArticle.Height = gbImagen.Height - 40;
				txtBarcode.Width = gbScan.Width - 90;
				lblMultiplier.Left = txtBarcode.Right + 5;
				lstSearch.Width = gbScan.Width - 20;
			}
			if (picLogo != null)
			{
				picLogo.Width = rightPanel.Width - 20;
			}
			cmbCustomer.Width = gbCliente.Width - 110;
			btnNewCustomer.Location = new Point(cmbCustomer.Right + 5, 29);
		};
		UpdateTotals();
	}

	private GroupBox CreateGroupBox(string title, int height)
	{
		return new GroupBox
		{
			Text = title,
			ForeColor = Color.LightGray,
			Height = height,
			Width = 400,
			Margin = new Padding(0, 0, 0, 15),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
	}

	private async void TxtBarcode_TextChanged(object? sender, EventArgs e)
	{
		string text = txtBarcode.Text.TrimStart();
		Match match = Regex.Match(text, "^(\\d+(?:[.,]\\d+)?)\\s*(?:\\*|x|X)\\s*(.*)");
		if (match.Success)
		{
			if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			{
				_nextQuantity = result;
				string text2 = match.Groups[2].Value.TrimStart();
				lblMultiplier.Text = $"x{_nextQuantity:G}";
				lblMultiplier.Visible = true;
				txtBarcode.Text = text2;
				txtBarcode.SelectionStart = txtBarcode.Text.Length;
				return;
			}
		}
		else if (text.Contains("*"))
		{
			string[] array = text.Split('*');
			if (array.Length == 2 && decimal.TryParse(array[0], out var result2))
			{
				_nextQuantity = result2;
				string text3 = array[1].TrimStart();
				lblMultiplier.Text = $"x{_nextQuantity:G}";
				lblMultiplier.Visible = true;
				txtBarcode.Text = text3;
				txtBarcode.SelectionStart = txtBarcode.Text.Length;
				return;
			}
		}
		string q = text.Trim();
		bool wildcardPresentations = false;
		if (q.StartsWith("/"))
		{
			wildcardPresentations = true;
			q = q.Substring(1).TrimStart();
		}

		if (q.Length >= 3)
		{
			try 
			{
				using LocalDbContext db = new LocalDbContext();
				string qLower = q.ToLower();
				var products = await db.Products.Where(p => p.Name.ToLower().Contains(qLower) || p.Barcode == q || p.InternalCode.ToString() == q).Take(10).ToListAsync();
				
				var productIds = products.Select(p => p.Id).ToList();
				
				// Traemos las presentaciones activas y filtramos en memoria para evitar problemas de traduccion de EF Core
				var allPresentations = await db.ProductPresentations.Where(p => p.IsActive).ToListAsync();
				
				// Encontramos presentaciones que coincidan directamente (por nombre o codigo de barras)
				var directPresentations = allPresentations.Where(p => p.Barcode == q || (p.Name != null && p.Name.ToLower().Contains(qLower))).ToList();
					
				var missingProductIds = directPresentations.Select(p => p.ProductId).Except(productIds).ToList();
				if (missingProductIds.Any())
				{
					products.AddRange(await db.Products.Where(p => missingProductIds.Contains(p.Id)).ToListAsync());
					productIds.AddRange(missingProductIds);
				}
				
				// Traemos TODAS las presentaciones de los productos que encontramos
				var productPresentations = allPresentations.Where(p => productIds.Contains(p.ProductId)).ToList();

				lstSearch.Items.Clear();
				foreach (Product item in products)
				{
					var itemPresentations = productPresentations.Where(p => p.ProductId == item.Id).ToList();
					
					if (wildcardPresentations)
					{
						// Si se usó el comodín, mostramos el producto base SOLO si no tiene presentaciones
						if (!itemPresentations.Any())
						{
							lstSearch.Items.Add(new ComboBoxItem($"{item.Name} - ${item.Price:N2}", item.Id));
						}
					}
					else
					{
						// Comportamiento normal: mostramos el producto base
						lstSearch.Items.Add(new ComboBoxItem($"{item.Name} - ${item.Price:N2}", item.Id));
					}
					
					// Mostramos las presentaciones
					foreach (var pres in itemPresentations)
					{
						string typeStr = pres.IsBulk ? $"[BULTO x{pres.Quantity:0.##}]" : $"[UNIDAD x{pres.Quantity:0.##}]";
						decimal price = pres.Price ?? (item.Price * pres.Quantity);
						string presText = pres.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase) ? "" : $" ({pres.Name})";
						lstSearch.Items.Add(new ComboBoxItem($"{typeStr} {item.Name}{presText} - ${price:N2}", item.Id, pres.Id));
					}
				}
				lstSearch.Visible = lstSearch.Items.Count > 0;
				if (lstSearch.Visible)
				{
					int preferredHeight = lstSearch.Items.Count * lstSearch.ItemHeight + 6;
					lstSearch.Height = Math.Min(preferredHeight, 250);
					
					// Find controlsPanel in rightPanel
					FlowLayoutPanel controlsPanel = (FlowLayoutPanel)lstSearch.Parent.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
					if (controlsPanel != null)
					{
						// Find gbScan inside controlsPanel
						GroupBox gbScan = controlsPanel.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text.Contains("Escanear"));
						if (gbScan != null)
						{
							lstSearch.Location = new Point(controlsPanel.Left + gbScan.Left + 10, controlsPanel.Top + gbScan.Top + 75);
						}
					}
					
					lstSearch.BringToFront();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error al buscar: {ex.Message}");
			}
		}
		else
		{
			lstSearch.Visible = false;
		}
	}

	private void LstSearch_DrawItem(object? sender, DrawItemEventArgs e)
	{
		if (e.Index < 0) return;
		e.DrawBackground();
		
		string text = lstSearch.Items[e.Index].ToString() ?? "";
		bool isBulto = text.Contains("[BULTO");
		
		Color foreColor = isBulto ? Color.FromArgb(255, 193, 7) : lstSearch.ForeColor;
		
		if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
		{
			foreColor = Color.White;
		}
		
		int splitIndex = text.LastIndexOf(" - $");
		if (splitIndex >= 0)
		{
			string namePart = text.Substring(0, splitIndex);
			string pricePart = text.Substring(splitIndex + 3); // keeps "$..."
			
			Rectangle rightBounds = e.Bounds;
			rightBounds.Width -= 10;
			
			TextRenderer.DrawText(e.Graphics, namePart, e.Font ?? lstSearch.Font, e.Bounds, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
			TextRenderer.DrawText(e.Graphics, pricePart, e.Font ?? lstSearch.Font, rightBounds, foreColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
		}
		else
		{
			TextRenderer.DrawText(e.Graphics, text, e.Font ?? lstSearch.Font, e.Bounds, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
		}
		
		e.DrawFocusRectangle();
	}

	private void LstSearch_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return && lstSearch.SelectedItem != null)
		{
			e.SuppressKeyPress = true;
			SelectSearchItem();
		}
	}

	private void LstSearch_DoubleClick(object? sender, EventArgs e)
	{
		if (lstSearch.SelectedItem != null)
		{
			SelectSearchItem();
		}
	}

	private async void SelectSearchItem()
	{
		if (!(lstSearch.SelectedItem is ComboBoxItem comboBoxItem))
		{
			return;
		}
		decimal qtyToAdd = _nextQuantity;
		using LocalDbContext db = new LocalDbContext();
		Product product = await db.Products.FindAsync(comboBoxItem.Value);
		if (product != null)
		{
			if (comboBoxItem.PresentationId.HasValue)
			{
				var pres = await db.ProductPresentations.FindAsync(comboBoxItem.PresentationId.Value);
				if (pres != null)
				{
					string typeStr = pres.IsBulk ? $"[BULTO x{pres.Quantity:0.##}]" : $"[UNIDAD x{pres.Quantity:0.##}]";
					string presText = pres.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase) ? "" : $" ({pres.Name})";
					string displayName = $"{typeStr} {product.Name}{presText}";
					
					decimal finalUnitPrice = (pres.Price.HasValue && pres.Quantity > 0) 
						? (pres.Price.Value / pres.Quantity) 
						: product.Price;
						
					decimal finalQuantity = qtyToAdd * pres.Quantity;
					
					AddRow(product.Id, displayName, finalUnitPrice, finalQuantity, product.Stock);
				}
			}
			else
			{
				AddRow(product.Id, product.Name, product.Price, qtyToAdd, product.Stock);
			}
			UpdateArticleImage(product.ImageUrl);
		}
		_nextQuantity = 1m;
		lblMultiplier.Visible = false;
		txtBarcode.Clear();
		lstSearch.Visible = false;
		txtBarcode.Focus();
	}



	private async void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Down && lstSearch.Visible && lstSearch.Items.Count > 0)
		{
			e.SuppressKeyPress = true;
			lstSearch.Focus();
			lstSearch.SelectedIndex = 0;
		}
		else
		{
			if (e.KeyCode != Keys.Return)
			{
				return;
			}
			e.SuppressKeyPress = true;
			string text = txtBarcode.Text.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				bool processed = await ProcessBarcodeAsync(text, _nextQuantity);
				
				if (!processed)
				{
					if (lstSearch.Visible && lstSearch.Items.Count > 0)
					{
						lstSearch.Focus();
						lstSearch.SelectedIndex = 0;
						SelectSearchItem();
					}
					else
					{
						MessageBox.Show("Producto o bulto no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						txtBarcode.Clear();
					}
				}
				else
				{
					_nextQuantity = 1m;
					lblMultiplier.Visible = false;
					txtBarcode.Clear();
					lstSearch.Visible = false;
				}
			}
		}
	}

	private async Task<bool> ProcessBarcodeAsync(string barcode, decimal quantity)
	{
		string barcode2 = barcode;
		using LocalDbContext db = new LocalDbContext();
		Product product = await db.Products.FirstOrDefaultAsync((Product p) => p.Barcode == barcode2 || p.InternalCode.ToString() == barcode2);
		
		if (product != null)
		{
			AddRow(product.Id, product.Name, product.Price, quantity, product.Stock);
			UpdateArticleImage(product.ImageUrl);
			return true;
		}

		// Buscar en presentaciones (bultos)
		var presentation = await db.ProductPresentations.FirstOrDefaultAsync(p => p.Barcode == barcode2);
		if (presentation != null)
		{
			var parentProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == presentation.ProductId);
			if (parentProduct != null)
			{
				string typeStr = presentation.IsBulk ? $"[BULTO x{presentation.Quantity:0.##}]" : $"[UNIDAD x{presentation.Quantity:0.##}]";
				string presText = presentation.Name.Equals(parentProduct.Name, StringComparison.OrdinalIgnoreCase) ? "" : $" ({presentation.Name})";
				string displayName = $"{typeStr} {parentProduct.Name}{presText}";
				decimal unitPrice = presentation.Price ?? parentProduct.Price;
				
				// Si la presentacin tiene precio fijo, lo dividimos por la cantidad que trae para mantener
				// la coherencia en la fila (que descuenta N unidades de stock), o simplemente
				// aadimos la fila as: el grid usa (Precio Unitario * Cantidad).
				// Como la presentacin trae N unidades, multiplicamos la cantidad a aadir por la cantidad del bulto,
				// PERO el "Precio Fijo" del bulto sera total. Para que en pantalla el precio sea correcto:
				// Precio Unitario en pantalla = PrecioBulto / CantidadUnidadesBulto.
				decimal finalUnitPrice = (presentation.Price.HasValue && presentation.Quantity > 0) 
					? (presentation.Price.Value / presentation.Quantity) 
					: parentProduct.Price;

				// La cantidad que se aade a la venta (y descuenta de stock) es cant Bultos * unidades por Bulto
				decimal finalQuantity = quantity * presentation.Quantity;

				AddRow(parentProduct.Id, displayName, finalUnitPrice, finalQuantity, parentProduct.Stock);
				UpdateArticleImage(parentProduct.ImageUrl);
				return true;
			}
		}

		return false;
	}

	private async Task CreateNewCustomerDialog()
	{
		Form modal = new Form();
		try
		{
			modal.Text = "Nuevo Cliente";
			modal.Size = new Size(400, 420);
			modal.StartPosition = FormStartPosition.CenterParent;
			modal.BackColor = Color.FromArgb(20, 20, 30);
			modal.ForeColor = Color.White;
			modal.FormBorderStyle = FormBorderStyle.FixedDialog;
			modal.MaximizeBox = false;
			modal.MinimizeBox = false;
			Label title = new Label
			{
				Text = "Crear/Buscar Cliente",
				Font = new Font("Segoe UI", 14f, FontStyle.Bold),
				ForeColor = Color.LightSkyBlue,
				AutoSize = true,
				Location = new Point(30, 20)
			};
			Label label = new Label
			{
				Text = "DNI / CUIT",
				Location = new Point(30, 60),
				AutoSize = true
			};
			TextBox txtDni = new TextBox
			{
				Location = new Point(30, 85),
				Width = 320,
				Font = new Font("Segoe UI", 12f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Label label2 = new Label
			{
				Text = "Nombre Completo",
				Location = new Point(30, 120),
				AutoSize = true
			};
			TextBox txtName = new TextBox
			{
				Location = new Point(30, 145),
				Width = 320,
				Font = new Font("Segoe UI", 12f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Label label3 = new Label
			{
				Text = "Email",
				Location = new Point(30, 180),
				AutoSize = true
			};
			TextBox txtEmail = new TextBox
			{
				Location = new Point(30, 205),
				Width = 320,
				Font = new Font("Segoe UI", 12f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Label label4 = new Label
			{
				Text = "Teléfono",
				Location = new Point(30, 240),
				AutoSize = true
			};
			TextBox txtPhone = new TextBox
			{
				Location = new Point(30, 265),
				Width = 320,
				Font = new Font("Segoe UI", 12f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Button button = new Button
			{
				Text = "Guardar y Seleccionar (Enter)",
				Location = new Point(30, 320),
				Width = 320,
				Height = 40,
				BackColor = Color.FromArgb(16, 185, 129),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold)
			};
			button.FlatAppearance.BorderSize = 0;
			modal.Controls.AddRange(title, label, txtDni, label2, txtName, label3, txtEmail, label4, txtPhone, button);
			txtDni.Leave += async delegate
			{
				string dni2 = txtDni.Text.Trim();
				if (string.IsNullOrEmpty(dni2))
				{
					return;
				}
				using LocalDbContext localDbContext = new LocalDbContext();
				Customer customer2 = await localDbContext.Customers.FirstOrDefaultAsync((Customer c) => c.Dni == dni2 || c.Cuit == dni2);
				if (customer2 != null)
				{
					txtName.Text = customer2.Name;
					txtEmail.Text = customer2.Email;
					txtPhone.Text = customer2.Phone;
					title.Text = "Cliente Existente";
					title.ForeColor = Color.YellowGreen;
				}
			};
			button.Click += async delegate
			{
				if (string.IsNullOrWhiteSpace(txtName.Text))
				{
					MessageBox.Show("El nombre es requerido.");
					return;
				}
				using LocalDbContext db = new LocalDbContext();
				string dni = txtDni.Text.Trim();
				Customer customer = ((!string.IsNullOrEmpty(dni)) ? (await db.Customers.FirstOrDefaultAsync((Customer c) => c.Dni == dni || c.Cuit == dni)) : null);
				Customer cust = customer;
				if (cust == null)
				{
					cust = new Customer
					{
						Name = txtName.Text.Trim(),
						Dni = (string.IsNullOrEmpty(dni) ? null : dni),
						Email = txtEmail.Text.Trim(),
						Phone = txtPhone.Text.Trim(),
						IsActive = true
					};
					db.Customers.Add(cust);
					db.Entry(cust).Property("IsSynced").CurrentValue = false;
					await db.SaveChangesAsync();
				}
				List<Customer> list = await db.Customers.ToListAsync();
				list.Insert(0, new Customer
				{
					Id = 0,
					Name = "Consumidor Final"
				});
				cmbCustomer.DataSource = list;
				cmbCustomer.SelectedValue = cust.Id;
				modal.DialogResult = DialogResult.OK;
				modal.Close();
			};
			modal.AcceptButton = button;
			modal.ShowDialog();
			txtBarcode.Focus();
		}
		finally
		{
			if (modal != null)
			{
				((IDisposable)modal).Dispose();
			}
			txtBarcode.Focus();
		}
	}

	private async Task ProcessDepartmentSale(Department dept)
	{
		Department dept2 = dept;
		using LocalDbContext db = new LocalDbContext();
		Product product = await db.Products.FirstOrDefaultAsync((Product p) => p.Id == dept2.VirtualProductId);
		if (product == null)
		{
			MessageBox.Show("Producto virtual del departamento no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		Form modal = new Form();
		try
		{
			modal.Text = "Venta por Departamento";
			modal.Size = new Size(400, 350);
			modal.StartPosition = FormStartPosition.CenterParent;
			modal.BackColor = Color.FromArgb(20, 20, 30);
			modal.ForeColor = Color.White;
			modal.FormBorderStyle = FormBorderStyle.FixedDialog;
			modal.MaximizeBox = false;
			modal.MinimizeBox = false;
			Label value = new Label
			{
				Text = "Venta por Departamento",
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				ForeColor = Color.MediumPurple,
				AutoSize = true,
				Location = new Point(50, 20)
			};
			Label value2 = new Label
			{
				Text = "Ingresando en " + dept2.Name,
				Font = new Font("Segoe UI", 10f),
				ForeColor = Color.LightGray,
				AutoSize = true,
				Location = new Point(90, 60)
			};
			Label value3 = new Label
			{
				Text = "\ud83c\udff7\ufe0f Precio a Cobrar ($)",
				Location = new Point(40, 100),
				AutoSize = true
			};
			TextBox txtPrice = new TextBox
			{
				Text = "0.00",
				Location = new Point(40, 125),
				Width = 300,
				Font = new Font("Segoe UI", 14f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Label value4 = new Label
			{
				Text = "\ud83d\udcac Descripción",
				Location = new Point(40, 170),
				AutoSize = true
			};
			TextBox textBox = new TextBox
			{
				Text = dept2.Name,
				Location = new Point(40, 195),
				Width = 300,
				Font = new Font("Segoe UI", 12f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Button button = new Button
			{
				Text = "+ Agregar",
				Location = new Point(80, 250),
				Width = 100,
				Height = 40,
				BackColor = Color.MediumSlateBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			button.FlatAppearance.BorderSize = 0;
			Button button2 = new Button
			{
				Text = "x Cancelar",
				Location = new Point(200, 250),
				Width = 100,
				Height = 40,
				BackColor = Color.Gray,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			button2.FlatAppearance.BorderSize = 0;
			modal.Controls.Add(value);
			modal.Controls.Add(value2);
			modal.Controls.Add(value3);
			modal.Controls.Add(txtPrice);
			modal.Controls.Add(value4);
			modal.Controls.Add(textBox);
			modal.Controls.Add(button);
			modal.Controls.Add(button2);
			modal.AcceptButton = button;
			modal.CancelButton = button2;
			button2.Click += delegate
			{
				modal.DialogResult = DialogResult.Cancel;
			};
			button.Click += delegate
			{
				if (decimal.TryParse(txtPrice.Text.Trim().Replace(".", ","), out var result) && result > 0m)
				{
					modal.DialogResult = DialogResult.OK;
				}
				else
				{
					MessageBox.Show("Ingrese un precio válido mayor a 0", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					txtPrice.Focus();
					txtPrice.SelectAll();
				}
			};
			modal.Shown += delegate
			{
				txtPrice.Focus();
				txtPrice.SelectAll();
			};
			if (modal.ShowDialog(this) == DialogResult.OK)
			{
				decimal price = Convert.ToDecimal(txtPrice.Text.Trim().Replace(".", ","));
				string name = (string.IsNullOrWhiteSpace(textBox.Text) ? dept2.Name : textBox.Text);
				AddRow(product.Id, name, price, 1m);
				UpdateArticleImage(null);
			}
		}
		finally
		{
			if (modal != null)
			{
				((IDisposable)modal).Dispose();
			}
			txtBarcode.Focus();
		}
	}

	private void GridItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = gridItems.Rows[e.RowIndex];
		decimal num = Convert.ToDecimal(dataGridViewRow.Cells["Quantity"].Value);
		decimal price = Convert.ToDecimal(dataGridViewRow.Cells["Price"].Value);
		if (e.ColumnIndex == gridItems.Columns["btnMinus"].Index)
		{
			if (num > 1m)
			{
				dataGridViewRow.Cells["Quantity"].Value = num - 1m;
				decimal newSubTotal = CalculateSubTotal((int)dataGridViewRow.Cells["Id"].Value, price, num - 1m);
				decimal newDiscount = (price * (num - 1m)) - newSubTotal;
				dataGridViewRow.Cells["Discount"].Value = newDiscount > 0m ? (object)newDiscount : null;
				dataGridViewRow.Cells["SubTotal"].Value = newSubTotal;
			}
			else
			{
				gridItems.Rows.RemoveAt(e.RowIndex);
			}
			UpdateTotals();
		}
		else if (e.ColumnIndex == gridItems.Columns["btnPlus"].Index)
		{
			dataGridViewRow.Cells["Quantity"].Value = num + 1m;
			decimal newSubTotal = CalculateSubTotal((int)dataGridViewRow.Cells["Id"].Value, price, num + 1m);
			decimal newDiscount = (price * (num + 1m)) - newSubTotal;
			dataGridViewRow.Cells["Discount"].Value = newDiscount > 0m ? (object)newDiscount : null;
			dataGridViewRow.Cells["SubTotal"].Value = newSubTotal;
			UpdateTotals();
		}
	}

	private decimal CalculateSubTotal(int productId, decimal price, decimal qty)
	{
		decimal presResult = 0m;
		decimal remainingQty = qty;
		
		if (_activePresentations != null && _activePresentations.Any())
		{
			var productPresentations = _activePresentations
				.Where(p => p.ProductId == productId && p.Price.HasValue && p.Quantity > 1)
				.OrderByDescending(p => p.Quantity)
				.ToList();

			foreach (var pres in productPresentations)
			{
				if (remainingQty >= pres.Quantity)
				{
					int bundles = (int)(remainingQty / pres.Quantity);
					presResult += bundles * pres.Price.Value;
					remainingQty -= bundles * pres.Quantity;
				}
			}
		}
		presResult += (remainingQty * price);

		decimal promoResult = price * qty;
		if (_activePromotions != null)
		{
			PromotionSyncDto promotionSyncDto = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds != null && p.ProductIds.Contains(productId));
			if (promotionSyncDto != null)
			{
				if (promotionSyncDto.Type == "XForY" && promotionSyncDto.BuyQuantity > 0 && promotionSyncDto.PayQuantity > 0)
				{
					int value = promotionSyncDto.BuyQuantity.Value;
					int value2 = promotionSyncDto.PayQuantity.Value;
					int num = (int)(qty / (decimal)value);
					decimal num2 = qty % (decimal)value;
					promoResult = ((decimal)(num * value2) + num2) * price;
				}
				else if (promotionSyncDto.Type == "Percentage" && promotionSyncDto.Value > 0m)
				{
					promoResult = (price * qty) * (1m - promotionSyncDto.Value / 100m);
				}
				else if (promotionSyncDto.Type == "FixedAmount" && promotionSyncDto.Value > 0m)
				{
					promoResult = qty * Math.Max(0m, price - promotionSyncDto.Value);
				}
			}
		}

		return Math.Min(presResult, promoResult);
	}

	private void AddRow(int id, string name, decimal price, decimal qty, decimal stock = 0m)
	{
		bool flag = false;
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			if ((int)item.Cells["Id"].Value == id && item.Cells["Name"].Value.ToString().StartsWith(name) && Convert.ToDecimal(item.Cells["Price"].Value) == price)
			{
				decimal num = Convert.ToDecimal(item.Cells["Quantity"].Value);
				item.Cells["Quantity"].Value = num + qty;
				decimal newSubTotal = CalculateSubTotal((int)item.Cells["Id"].Value, price, num + qty);
				decimal newDiscount = (price * (num + qty)) - newSubTotal;
				item.Cells["Discount"].Value = newDiscount > 0m ? (object)newDiscount : null;
				item.Cells["SubTotal"].Value = newSubTotal;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			string text = $"{name} ({stock:0.##})";
			decimal subTotal = CalculateSubTotal(id, price, qty);
			decimal discount = (price * qty) - subTotal;
			int rowIndex = gridItems.Rows.Add(id, text, text, price, "-", qty, "+", discount > 0m ? (object)discount : null, subTotal);
			if (name.Contains("[BULTO"))
			{
				gridItems.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(255, 193, 7);
			}
		}
		UpdateTotals();
	}

	private void UpdateTotals()
	{
		decimal value = default(decimal);
		decimal grossTotal = default(decimal);
		decimal num = 0m;
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			value += Convert.ToDecimal(item.Cells["SubTotal"].Value);
			decimal qty = Convert.ToDecimal(item.Cells["Quantity"].Value);
			decimal price = Convert.ToDecimal(item.Cells["Price"].Value);
			grossTotal += (qty * price);
			num += qty;
		}
		
		decimal discount = grossTotal - value;
		if (discount > 0)
		{
		    lblPromoDiscountValue.Text = $"-${discount:N2}";
		    lblPromoDiscountText.Visible = true;
		    lblPromoDiscountValue.Visible = true;
		}
		else
		{
		    lblPromoDiscountValue.Text = "$0.00";
		    lblPromoDiscountText.Visible = false;
		    lblPromoDiscountValue.Visible = false;
		}
		
		lblTotal.Text = $"${value:N2}";
		lblSubTotalValue.Text = $"${grossTotal:N2}";
		lblItemsCount.Text = $"Cantidad de Artículos: {num.ToString("0.##")}";
		lblVuelto.Text = $"Resta: -${value:N2}";
		if (lblTotal.Parent != null)
		{
			lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
			lblTotalText.Left = lblTotal.Left - lblTotalText.Width - 10;
			
			lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
			lblPromoDiscountValue.Left = lblTotal.Parent.Width - lblPromoDiscountValue.Width - 10;
			
			int maxValWidth = Math.Max(lblSubTotalValue.Width, lblPromoDiscountText.Visible ? lblPromoDiscountValue.Width : 0);
			int textRightEdge = lblTotal.Parent.Width - maxValWidth - 20; // 10 margin + 10 gap
			
			lblSubTotalText.Left = textRightEdge - lblSubTotalText.Width;
			lblPromoDiscountText.Left = textRightEdge - lblPromoDiscountText.Width;
		}
		if (lblVuelto.Parent != null)
		{
			lblVuelto.Left = lblVuelto.Parent.Width - lblVuelto.Width - 10;
		}
	}

	private void UpdateArticleImage(string? imageUrl)
	{
		string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "logo.png");
		if (!string.IsNullOrWhiteSpace(imageUrl))
		{
			string text2 = imageUrl.TrimStart('/', '\\');
			string text3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", text2);
			if (!File.Exists(text3))
			{
				text3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, text2);
			}
			if (File.Exists(text3))
			{
				text = text3;
			}
		}
		try
		{
			if (File.Exists(text))
			{
				Image image = picArticle.Image;
				picArticle.Image = Image.FromFile(text);
				image?.Dispose();
			}
		}
		catch
		{
		}
	}

	private async void BtnFinalize_Click(object? sender, EventArgs e)
	{
		if (gridItems.Rows.Count == 0)
		{
			return;
		}
		decimal total = default(decimal);
		decimal num = default(decimal);
		Sale sale = new Sale
		{
			GlobalId = Guid.NewGuid(),
			Date = DateTime.Now,
			IsSynced = false,
			CustomerId = ((cmbCustomer.SelectedValue as int? == 0) ? null : ((int?)cmbCustomer.SelectedValue)),
			UserId = _userId,
			CashRegisterId = _cashRegisterId,
			RequestElectronicInvoice = _requestElectronicInvoice
		};
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			int productId = Convert.ToInt32(item.Cells["Id"].Value);
			decimal num2 = Convert.ToDecimal(item.Cells["Price"].Value);
			decimal num3 = Convert.ToDecimal(item.Cells["Quantity"].Value);
			num += num2 * num3;
			total += CalculateSubTotal(productId, num2, num3);
			sale.Items.Add(new SaleItem
			{
				ProductId = productId,
				UnitPrice = num2,
				Quantity = num3
			});
		}
		sale.SubTotal = num;
		sale.DiscountAmount = num - total;
		sale.TotalAmount = total;
		Form modal = new Form();
		try
		{
			modal.Text = "Finalizar Venta";
			modal.Size = new Size(400, 390);
			modal.StartPosition = FormStartPosition.CenterParent;
			modal.BackColor = Color.FromArgb(20, 20, 30);
			modal.ForeColor = Color.White;
			modal.FormBorderStyle = FormBorderStyle.FixedDialog;
			modal.MaximizeBox = false;
			modal.MinimizeBox = false;
			Label title = new Label
			{
				Text = "Medio de Pago",
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				ForeColor = Color.LightSkyBlue,
				AutoSize = true,
				Location = new Point(40, 20)
			};
			Label lblTotalText = new Label
			{
				Text = $"Total a cobrar: ${total:N2}",
				Font = new Font("Segoe UI", 14f, FontStyle.Bold),
				ForeColor = Color.YellowGreen,
				AutoSize = true,
				Location = new Point(40, 60)
			};
			Label lblMethod = new Label
			{
				Text = "Medio de Pago",
				Location = new Point(40, 115),
				AutoSize = true
			};
			ComboBox localCmbPaymentMethod = new ComboBox
			{
				Location = new Point(40, 140),
				Width = 300,
				Font = new Font("Segoe UI", 12f),
				DropDownStyle = ComboBoxStyle.DropDownList,
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White
			};
			using LocalDbContext db = new LocalDbContext();
			List<PaymentMethod> list = await db.PaymentMethods.Where((PaymentMethod p) => p.IsActive).ToListAsync();
			if (list.Count == 0)
			{
				list.Add(new PaymentMethod
				{
					Id = 1,
					Name = "Efectivo"
				});
			}
			foreach (var pm in list)
			{
				localCmbPaymentMethod.Items.Add(pm);
			}
			localCmbPaymentMethod.DisplayMember = "Name";
			localCmbPaymentMethod.ValueMember = "Id";
			
			var defaultPaymentSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DefaultPaymentMethodId");
			bool wasSet = false;
			if (defaultPaymentSetting != null && int.TryParse(defaultPaymentSetting.Value, out int defaultMethodId))
			{
				var item = list.FirstOrDefault(p => p.Id == defaultMethodId);
				if (item != null)
				{
					localCmbPaymentMethod.SelectedItem = item;
					wasSet = true;
				}
			}
			
			// Si no logramos seleccionar por configuración
			if (!wasSet)
			{
				// Fallback to Efectivo if exists
				var efectivo = list.FirstOrDefault(p => p.Name.ToLower() == "efectivo");
				if (efectivo != null)
				{
					localCmbPaymentMethod.SelectedItem = efectivo;
				}
			}


			Label label = new Label
			{
				Text = "Paga con ($)",
				Location = new Point(40, 185),
				AutoSize = true
			};
			TextBox txtPagaCon = new TextBox
			{
				Text = total.ToString("0.00"),
				Location = new Point(40, 210),
				Width = 140,
				Font = new Font("Segoe UI", 14f),
				BackColor = Color.FromArgb(30, 30, 45),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle
			};
			Label lblVueltoModal = new Label
			{
				Text = "Vuelto: $0.00",
				Location = new Point(200, 210),
				AutoSize = true,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				ForeColor = Color.Gold
			};
			Button button = new Button
			{
				Text = "✔\ufe0f CONFIRMAR [Enter]",
				Location = new Point(40, 280),
				Width = 300,
				Height = 40,
				BackColor = Color.FromArgb(16, 185, 129),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold)
			};
			button.FlatAppearance.BorderSize = 0;
			modal.Controls.AddRange(title, lblTotalText, lblMethod, localCmbPaymentMethod, label, txtPagaCon, lblVueltoModal, button);
			localCmbPaymentMethod.SelectedIndexChanged += delegate
			{
				if (localCmbPaymentMethod.SelectedItem is PaymentMethod { DiscountPercentage: var num5 } paymentMethod)
				{
					DateTime date = DateTime.Now.Date;
					if (paymentMethod.DiscountValidFrom.HasValue && date < paymentMethod.DiscountValidFrom.Value.Date)
					{
						num5 = default(decimal);
					}
					if (paymentMethod.DiscountValidTo.HasValue && date > paymentMethod.DiscountValidTo.Value.Date)
					{
						num5 = default(decimal);
					}
					sale.PaymentDiscountAmount = total * (num5 / 100m);
					sale.TotalAmount = total - sale.PaymentDiscountAmount;
					lblTotalText.Text = $"Total a cobrar: ${sale.TotalAmount:N2}";
					if (sale.PaymentDiscountAmount > 0m)
					{
						lblTotalText.Text += $"\n(Desc: -${sale.PaymentDiscountAmount:N2})";
					}
					txtPagaCon.Text = sale.TotalAmount.ToString("0.00");
				}
			};
			
			txtPagaCon.TextChanged += delegate
			{
				if (decimal.TryParse(txtPagaCon.Text.Replace(".", ","), out var result))
				{
					decimal num4 = result - sale.TotalAmount;
					lblVueltoModal.Text = ((num4 >= 0m) ? $"Vuelto: ${num4:N2}" : $"Falta: ${Math.Abs(num4):N2}");
					lblVueltoModal.ForeColor = ((num4 >= 0m) ? Color.Gold : Color.Tomato);
				}
			};
			button.Click += async delegate
			{
				sale.Payments.Add(new SalePayment
				{
					PaymentMethodId = ((PaymentMethod)localCmbPaymentMethod.SelectedItem).Id,
					Amount = sale.TotalAmount
				});
				sale.DiscountAmount += sale.PaymentDiscountAmount;
				using LocalDbContext context = new LocalDbContext();
				context.Sales.Add(sale);
				await context.SaveChangesAsync();
				string text = localCmbPaymentMethod.Text;
				string text2 = GenerateTicketText(sale, text);
				PrintTicket(text2);
				ShowAutoCloseMessage($"Venta registrada exitosamente.\nTicket: {_posNumber:D5}-{sale.Id:D8}\n\nImprimiendo Ticket...", "Caja", 1500);
				gridItems.Rows.Clear();
				UpdateArticleImage(null);
				UpdateTotals();
				modal.DialogResult = DialogResult.OK;
				modal.Close();
				Task.Run(async delegate
				{
					try
					{
						await _syncWorker.PerformSyncAsync();
					}
					catch (Exception)
					{
						Invoke((MethodInvoker)delegate
						{
							btnSync.Text = "⚠ OFFLINE (Local)";
							btnSync.ForeColor = Color.Red;
						});
					}
				});
			};
			modal.AcceptButton = button;
			modal.Shown += delegate
			{
				txtPagaCon.Focus();
				txtPagaCon.SelectAll();
			};
			modal.FormClosed += delegate
			{
				txtBarcode.Focus();
			};
			modal.ShowDialog();
		}
		finally
		{
			if (modal != null)
			{
				((IDisposable)modal).Dispose();
			}
			txtBarcode.Focus();
		}
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		switch (keyData)
		{
		case Keys.F1:
			txtBarcode.Focus();
			return true;
		case Keys.F11:
			ToggleInvoiceType();
			return true;
		case Keys.F12:
			BtnFinalize_Click(this, EventArgs.Empty);
			return true;
		case Keys.F5:
			CreateNewCustomerDialog();
			return true;
		default:
		{
			if (_departmentHotkeys.TryGetValue(keyData, out Department value))
			{
				ProcessDepartmentSale(value);
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
		}
	}

	private void PrintTicket(string text, string printerName = "", int copies = 1)
	{
		string text2 = text;
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		try
		{
			PrintDocument printDocument = new PrintDocument();
			if (!string.IsNullOrEmpty(printerName))
			{
				printDocument.PrinterSettings.PrinterName = printerName;
			}
			printDocument.PrintPage += delegate(object s, PrintPageEventArgs e)
			{
				Font font = new Font("Courier New", 8f, FontStyle.Bold);
				float num = 0f;
				int num2 = 0;
				float x = 0f;
				float num3 = 0f;
				if (e.Graphics != null)
				{
					string[] array = text2.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
					foreach (string s2 in array)
					{
						num = num3 + (float)num2 * font.GetHeight(e.Graphics);
						e.Graphics.DrawString(s2, font, Brushes.Black, x, num, new StringFormat());
						num2++;
					}
				}
			};
			for (int i = 0; i < copies; i++)
			{
				printDocument.Print();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al intentar imprimir el ticket: " + ex.Message, "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void ShowAutoCloseMessage(string message, string title, int timeoutMs)
	{
		Form form = new Form
		{
			Text = title,
			Size = new Size(400, 150),
			StartPosition = FormStartPosition.CenterParent,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			MaximizeBox = false,
			MinimizeBox = false,
			ControlBox = false,
			BackColor = Color.FromArgb(20, 20, 30),
			ForeColor = Color.White
		};
		Label label = new Label
		{
			Text = message,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleCenter,
			Font = new Font("Segoe UI", 12f)
		};
		form.Controls.Add(label);
		System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
		{
			Interval = timeoutMs
		};
		timer.Tick += delegate
		{
			timer.Stop();
			form.Close();
		};
		timer.Start();
		form.ShowDialog(this);
	}

	private string GenerateTicketText(Sale sale, string paymentMethodName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("         TICKET DE VENTA");
		stringBuilder.AppendLine("=================================");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("Fecha: ");
		handler.AppendFormatted(sale.Date, "dd/MM/yyyy HH:mm");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
		handler.AppendLiteral("Ticket: ");
		handler.AppendFormatted(_posNumber, "D5");
		handler.AppendLiteral("-");
		handler.AppendFormatted(sale.Id, "D8");
		stringBuilder4.AppendLine(ref handler);
		string value = "Cajero";
		if (!string.IsNullOrEmpty(Text) && Text.Contains("Cajero: "))
		{
			int num = Text.IndexOf("Cajero: ") + 8;
			int num2 = Text.IndexOf(" -", num);
			if (num2 > num)
			{
				value = Text.Substring(num, num2 - num);
			}
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
		handler.AppendLiteral("Cajero: ");
		handler.AppendFormatted(value);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine("---------------------------------");
		stringBuilder.AppendLine("Cant  Descripcion         Importe");
		foreach (SaleItem item2 in sale.Items)
		{
			SaleItem item = item2;
			using LocalDbContext localDbContext = new LocalDbContext();
			Product product = localDbContext.Products.FirstOrDefault((Product p) => p.Id == item.ProductId);
			string text = ((product != null) ? product.Name : "Producto");
			if (text.Length > 18)
			{
				text = text.Substring(0, 18);
			}
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 3, stringBuilder2);
			handler.AppendFormatted(item.Quantity, 4);
			handler.AppendLiteral(" ");
			handler.AppendFormatted<string>(text, -18);
			handler.AppendLiteral(" $");
			handler.AppendFormatted(item.Quantity * item.UnitPrice, 7, "0.00");
			stringBuilder6.AppendLine(ref handler);
		}
		stringBuilder.AppendLine("---------------------------------");
		if (sale.PaymentDiscountAmount > 0m)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("SUBTOTAL:               $");
			handler.AppendFormatted(sale.SubTotal, 7, "0.00");
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("DESC. PAGO:            -$");
			handler.AppendFormatted(sale.PaymentDiscountAmount, 7, "0.00");
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("TOTAL:                  $");
			handler.AppendFormatted(sale.TotalAmount, 7, "0.00");
			stringBuilder9.AppendLine(ref handler);
		}
		else if (sale.DiscountAmount > 0m)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("SUBTOTAL:               $");
			handler.AppendFormatted(sale.SubTotal, 7, "0.00");
			stringBuilder10.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("DESCUENTO:             -$");
			handler.AppendFormatted(sale.DiscountAmount, 7, "0.00");
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("TOTAL:                  $");
			handler.AppendFormatted(sale.TotalAmount, 7, "0.00");
			stringBuilder12.AppendLine(ref handler);
		}
		else
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
			handler.AppendLiteral("TOTAL:                  $");
			handler.AppendFormatted(sale.TotalAmount, 7, "0.00");
			stringBuilder13.AppendLine(ref handler);
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder14 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("Medio de Pago: ");
		handler.AppendFormatted(paymentMethodName);
		stringBuilder14.AppendLine(ref handler);
		stringBuilder.AppendLine("=================================");
		stringBuilder.AppendLine("      GRACIAS POR SU COMPRA");
		stringBuilder.AppendLine("\n\n\n\n\n\n");
		return stringBuilder.ToString();
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		_syncWorker.Stop();
		base.OnFormClosing(e);
	}
}
