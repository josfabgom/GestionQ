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

		public ComboBoxItem(string text, int value)
		{
			Text = text;
			Value = value;
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
			PosStatusResponseDto posStatusResponseDto = await _authClient.GetStatusAsync(_posIdentifier);
			_posNumber = posStatusResponseDto.PosNumber.GetValueOrDefault(1);
			using (LocalDbContext localDbContext = new LocalDbContext())
			{
				SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosNumber");
				if (systemSetting == null)
				{
					localDbContext.SystemSettings.Add(new SystemSetting
					{
						Key = "PosNumber",
						Value = _posNumber.ToString()
					});
				}
				else
				{
					systemSetting.Value = _posNumber.ToString();
				}
				localDbContext.SaveChanges();
			}
			if (posStatusResponseDto.HasOpenRegister)
			{
				if (posStatusResponseDto.UserId != _userId)
				{
					MessageBox.Show("La caja actual fue abierta por " + posStatusResponseDto.UserName + ". Para operar, inicie sesión con su usuario, o cierre la caja.", "Caja Ocupada", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					Application.Exit();
					return;
				}
				_cashRegisterId = posStatusResponseDto.CashRegisterId.Value;
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
				Width = 100,
				Height = 60,
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
			RowCount = 1,
			Padding = new Padding(20)
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
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
			new SalesHistoryForm(_cashRegisterId, _posNumber).ShowDialog(this);
		};
		flowLayoutPanel.Controls.Add(btnSync);
		flowLayoutPanel.Controls.Add(btnSettings);
		flowLayoutPanel.Controls.Add(btnCloseRegister);
		flowLayoutPanel.Controls.Add(btnPartialReport);
		flowLayoutPanel.Controls.Add(btnSalesHistory);
		flowLayoutPanel.Controls.Add(btnAddMovement);
		panel2.Controls.Add(flowLayoutPanel);
		panel2.Controls.Add(lblTitle);
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
			Font = new Font("Segoe UI", 14f, FontStyle.Bold)
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
		totalBox.Paint += delegate(object? s, PaintEventArgs e)
		{
			ControlPaint.DrawBorder(e.Graphics, totalBox.ClientRectangle, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid);
		};
		lblSubTotalText = new Label
		{
			Text = "SubTotal:",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 150, 15),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblSubTotalValue = new Label
		{
			Text = "$0.00",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 15),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblTotalText = new Label
		{
			Text = "TOTAL A COBRAR:",
			ForeColor = greenColor,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 250, 60),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblTotal = new Label
		{
			Text = "$0.00",
			ForeColor = greenColor,
			Font = new Font("Segoe UI", 36f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 160, 45),
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
			Font = new Font("Segoe UI", 12f)
		};
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
		gbScan.Controls.Add(lstSearch);
		GroupBox gbImagen = CreateGroupBox("Imagen del Artículo", 230);
		gbImagen.Location = new Point(0, gbScan.Bottom + 10);
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
		leftSplit.Controls.Add(gbScan);
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
		panel.Controls.Add(panel4);
		tableLayoutPanel.Controls.Add(panel, 0, 0);
		tableLayoutPanel.Controls.Add(rightPanel, 1, 0);
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
				leftSplit.Width = (int)((double)splitPanel.Width * 0.6);
				rightSplit.Width = splitPanel.Width - leftSplit.Width;
				gbScan.Width = leftSplit.Width - 10;
				gbImagen.Width = leftSplit.Width - 10;
				gbImagen.Height = splitPanel.Height - gbScan.Height - 10;
				gbDepts.Height = splitPanel.Height;
				gbImagen.Width = leftSplit.Width - 10;
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
		if (q.Length >= 3)
		{
			using LocalDbContext db = new LocalDbContext();
			string qLower = q.ToLower();
			List<Product> list = await db.Products.Where((Product p) => p.Name.ToLower().Contains(qLower) || p.Barcode == q || p.InternalCode.ToString() == q).Take(10).ToListAsync();
			lstSearch.Items.Clear();
			foreach (Product item in list)
			{
				lstSearch.Items.Add(new ComboBoxItem(item.Name, item.Id));
			}
			lstSearch.Visible = list.Count > 0;
		}
		else
		{
			lstSearch.Visible = false;
		}
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
			AddRow(product.Id, product.Name, product.Price, qtyToAdd, product.Stock);
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
				if (lstSearch.Visible && lstSearch.Items.Count > 0)
				{
					lstSearch.Focus();
					lstSearch.SelectedIndex = 0;
					SelectSearchItem();
					return;
				}
				decimal nextQuantity = _nextQuantity;
				string barcode = text;
				_nextQuantity = 1m;
				lblMultiplier.Visible = false;
				txtBarcode.Clear();
				lstSearch.Visible = false;
				await ProcessBarcodeAsync(barcode, nextQuantity);
			}
		}
	}

	private async Task ProcessBarcodeAsync(string barcode, decimal quantity)
	{
		string barcode2 = barcode;
		using LocalDbContext db = new LocalDbContext();
		Product product = await db.Products.FirstOrDefaultAsync((Product p) => p.Barcode == barcode2 || p.InternalCode.ToString() == barcode2);
		if (product != null)
		{
			AddRow(product.Id, product.Name, product.Price, quantity, product.Stock);
			UpdateArticleImage(product.ImageUrl);
		}
		else
		{
			MessageBox.Show("Producto no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
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
				dataGridViewRow.Cells["SubTotal"].Value = CalculateSubTotal((int)dataGridViewRow.Cells["Id"].Value, price, num - 1m);
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
			dataGridViewRow.Cells["SubTotal"].Value = CalculateSubTotal((int)dataGridViewRow.Cells["Id"].Value, price, num + 1m);
			UpdateTotals();
		}
	}

	private decimal CalculateSubTotal(int productId, decimal price, decimal qty)
	{
		decimal result = price * qty;
		if (_activePromotions == null)
		{
			return result;
		}
		PromotionSyncDto promotionSyncDto = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds != null && p.ProductIds.Contains(productId));
		if (promotionSyncDto == null)
		{
			return result;
		}
		if (promotionSyncDto.Type == "XForY" && promotionSyncDto.BuyQuantity > 0 && promotionSyncDto.PayQuantity > 0)
		{
			int value = promotionSyncDto.BuyQuantity.Value;
			int value2 = promotionSyncDto.PayQuantity.Value;
			int num = (int)(qty / (decimal)value);
			decimal num2 = qty % (decimal)value;
			result = ((decimal)(num * value2) + num2) * price;
		}
		else if (promotionSyncDto.Type == "Percentage" && promotionSyncDto.Value > 0m)
		{
			result *= 1m - promotionSyncDto.Value / 100m;
		}
		else if (promotionSyncDto.Type == "FixedAmount" && promotionSyncDto.Value > 0m)
		{
			result = qty * Math.Max(0m, price - promotionSyncDto.Value);
		}
		return result;
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
				item.Cells["SubTotal"].Value = CalculateSubTotal((int)item.Cells["Id"].Value, price, num + qty);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			string text = $"{name} ({stock:0.##})";
			gridItems.Rows.Add(id, text, text, price, "-", qty, "+", CalculateSubTotal(id, price, qty));
		}
		UpdateTotals();
	}

	private void UpdateTotals()
	{
		decimal value = default(decimal);
		int num = 0;
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			value += Convert.ToDecimal(item.Cells["SubTotal"].Value);
			num++;
		}
		lblTotal.Text = $"${value:N2}";
		lblItemsCount.Text = $"Cantidad de Artículos: {num}";
		lblVuelto.Text = $"Resta: -${value:N2}";
		if (lblTotal.Parent != null)
		{
			lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
			lblTotalText.Left = lblTotal.Left - lblTotalText.Width - 10;
			lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
			lblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
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
			localCmbPaymentMethod.DataSource = list;
			localCmbPaymentMethod.DisplayMember = "Name";
			localCmbPaymentMethod.ValueMember = "Id";
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
			if (localCmbPaymentMethod.Items.Count > 0)
			{
				int selectedIndex = localCmbPaymentMethod.SelectedIndex;
				localCmbPaymentMethod.SelectedIndex = -1;
				localCmbPaymentMethod.SelectedIndex = ((selectedIndex >= 0) ? selectedIndex : 0);
			}
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
					PaymentMethodId = (int)localCmbPaymentMethod.SelectedValue,
					Amount = sale.TotalAmount
				});
				sale.DiscountAmount += sale.PaymentDiscountAmount;
				using LocalDbContext context = new LocalDbContext();
				context.Sales.Add(sale);
				await context.SaveChangesAsync();
				string text = localCmbPaymentMethod.Text;
				string text2 = GenerateTicketText(sale, text);
				PrintTicket(text2);
				if (MessageBox.Show($"Venta registrada exitosamente.\nTicket: {_posNumber:D5}-{sale.Id:D8}\n\n¿Desea imprimir una copia del ticket?", "Caja", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
				{
					PrintTicket(text2);
				}
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
