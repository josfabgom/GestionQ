using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GestionQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GestionQ.CajaPOS
{
    public partial class Form1 : Form
    {
        // Modern dark mode colors (Tailwind style)
        private Color bgColor = Color.FromArgb(17, 24, 39);        // gray-900
        private Color panelColor = Color.FromArgb(31, 41, 55);     // gray-800
        private Color accentColor = Color.FromArgb(139, 92, 246);  // violet-500
        private Color greenColor = Color.FromArgb(16, 185, 129);   // emerald-500
        private Color textColor = Color.White;
        
        private DataGridView gridItems = new();
        private Label lblTotal = new();
        private Label lblItemsCount = new();
        private ComboBox cmbCustomer = new();
        private ComboBox cmbPaymentMethod = new();
        private TextBox txtBarcode = new();
        private Dictionary<Keys, Department> _departmentHotkeys = new();
        private ListBox lstSearch = new();
        private FlowLayoutPanel panelDepartments = new();
        private Label lblVuelto = new();
        private Button btnSync = new();
        private Label lblMultiplier = new();
        private decimal _nextQuantity = 1;
        
        private SyncWorker _syncWorker;
        private AuthClient _authClient;
        private string _userId;
        private int _cashRegisterId;
        private string _posIdentifier = Environment.MachineName;
        private Button btnCloseRegister = new();
        private Button btnAddMovement = new();
        
        private bool _requestElectronicInvoice = false;
        private Label lblTitle = new();
        
        private void ToggleInvoiceType()
        {
            _requestElectronicInvoice = !_requestElectronicInvoice;
            if (_requestElectronicInvoice)
            {
                lblTitle.ForeColor = Color.DeepSkyBlue;
                lblTitle.Text = "📠 Punto de Venta (Caja)   ●";
            }
            else
            {
                lblTitle.ForeColor = Color.WhiteSmoke;
                lblTitle.Text = "📠 Punto de Venta (Caja)";
            }
        }

        public Form1()
        {
            _authClient = new AuthClient("http://localhost:5144");
            InitializeUI();
            
            _syncWorker = new SyncWorker("http://localhost:5144");
            _syncWorker.OnSyncCompleted += () => {
                this.Invoke((MethodInvoker)delegate {
                    LoadInitialDataAsync();
                    btnSync.Text = "Sincronizado ✔️";
                    btnSync.ForeColor = greenColor;
                });
            };
            _syncWorker.OnSyncError += (errorMsg) => {
                this.Invoke((MethodInvoker)delegate {
                    btnSync.Text = "⚠️ Error Sync";
                    btnSync.ForeColor = Color.Red;
                });
            };
            
            this.Shown += Form1_Shown;
        }

        private async void Form1_Shown(object? sender, EventArgs e)
        {
            this.Hide(); // Ocultar UI principal hasta hacer login

            var loginForm = new LoginForm(_authClient, _posIdentifier);
            if (loginForm.ShowDialog(this) != DialogResult.OK)
            {
                Application.Exit();
                return;
            }

            _userId = loginForm.LoginResult.UserId;

            try
            {
                var status = await _authClient.GetStatusAsync(_posIdentifier);
                if (status.HasOpenRegister)
                {
                    if (status.UserId != _userId)
                    {
                        MessageBox.Show($"La caja actual fue abierta por {status.UserName}. Para operar, inicie sesión con su usuario, o cierre la caja.", "Caja Ocupada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Exit();
                        return;
                    }
                    _cashRegisterId = status.CashRegisterId.Value;
                }
                else
                {
                    var openForm = new OpenRegisterForm(_authClient, _userId, _posIdentifier);
                    if (openForm.ShowDialog(this) != DialogResult.OK)
                    {
                        Application.Exit();
                        return;
                    }
                    _cashRegisterId = openForm.OpenResult.CashRegisterId.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al verificar estado de caja con el servidor. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            this.Show();
            
            string displayName = !string.IsNullOrEmpty(loginForm.LoginResult.FullName) ? loginForm.LoginResult.FullName : loginForm.LoginResult.UserName;
            
            this.Text = $"GestionQ - Punto de Venta (Caja) - Cajero: {displayName} - Caja: {_cashRegisterId}";

            _syncWorker.Start();
            LoadInitialDataAsync();
        }

        private async void LoadInitialDataAsync()
        {
            using var db = new LocalDbContext();
            db.Database.EnsureCreated();
            try {
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "ALTER TABLE Sales ADD COLUMN RequestElectronicInvoice INTEGER NOT NULL DEFAULT 0;");
            } catch { }
            
            var customers = await db.Customers.ToListAsync();
            customers.Insert(0, new Customer { Id = 0, Name = "Consumidor Final" });
            
            var selectedCustomerId = cmbCustomer.SelectedValue;
            cmbCustomer.DataSource = customers;
            cmbCustomer.DisplayMember = "Name";
            cmbCustomer.ValueMember = "Id";
            if (selectedCustomerId != null) cmbCustomer.SelectedValue = selectedCustomerId;

            var paymentMethods = await db.PaymentMethods.Where(p => p.IsActive).ToListAsync();
            if (paymentMethods.Count == 0) paymentMethods.Add(new PaymentMethod { Id = 1, Name = "Efectivo" });
            
            var selectedPmId = cmbPaymentMethod.SelectedValue;
            cmbPaymentMethod.DataSource = paymentMethods;
            cmbPaymentMethod.DisplayMember = "Name";
            cmbPaymentMethod.ValueMember = "Id";
            if (selectedPmId != null) cmbPaymentMethod.SelectedValue = selectedPmId;

            var departments = await db.Departments.ToListAsync();
            panelDepartments.Controls.Clear();
            _departmentHotkeys.Clear();
            
            foreach(var dept in departments)
            {
                if (!string.IsNullOrEmpty(dept.Hotkey) && Enum.TryParse<Keys>(dept.Hotkey, true, out var key))
                {
                    _departmentHotkeys[key] = dept;
                }

                var btn = new Button
                {
                    Text = string.IsNullOrEmpty(dept.Hotkey) ? dept.Name : $"{dept.Name}\n[{dept.Hotkey}]",
                    Width = 100, Height = 60,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = textColor,
                    BackColor = bgColor,
                    Font = new Font("Segoe UI", 9)
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
                btn.Tag = dept;
                btn.Click += async (s, e) => await ProcessDepartmentSale(dept);
                panelDepartments.Controls.Add(btn);
            }
            
            if (departments.Count == 0)
            {
                var lbl = new Label { Text = "No hay departamentos", ForeColor = Color.Gray, AutoSize = true };
                panelDepartments.Controls.Add(lbl);
            }
        }

        private void InitializeUI()
        {
            this.Text = "GestionQ - Punto de Venta (Caja)";
            this.Size = new Size(1366, 768);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.Font = new Font("Segoe UI", 10);
            
            TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(20) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 20, 0) };
            
            Panel headerLeft = new Panel { Dock = DockStyle.Top, Height = 50 };
            lblTitle.Text = "📠 Punto de Venta (Caja)";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(0, 10);
            lblTitle.ForeColor = Color.WhiteSmoke;
            lblItemsCount = new Label { Text = "0 ítems cargados", ForeColor = accentColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(leftPanel.Width - 150, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            btnSync = new Button { Text = "🔄 Sincronizar", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = textColor, Location = new Point(leftPanel.Width - 300, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSync.FlatAppearance.BorderSize = 0;
            btnSync.Click += async (s, e) => {
                btnSync.Text = "⏳ Sincronizando...";
                btnSync.ForeColor = Color.Yellow;
                try
                {
                    await _syncWorker.PerformSyncAsync();
                }
                catch (Exception)
                {
                    btnSync.Text = "⚠️ Error Sync";
                    btnSync.ForeColor = Color.Red;
                    MessageBox.Show("No se pudo conectar con la central.\nRevise su conexión o asegúrese de que el servidor esté activo.", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnCloseRegister = new Button { Text = "🔒 Cerrar Caja", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(239, 68, 68), Location = new Point(leftPanel.Width - 450, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCloseRegister.FlatAppearance.BorderSize = 0;
            btnCloseRegister.Click += async (s, e) => {
                var closeForm = new CloseRegisterForm(_authClient, _cashRegisterId);
                if (closeForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(closeForm.TicketText)) 
                    {
                        PrintTicket(closeForm.TicketText);
                    }
                    MessageBox.Show("Caja cerrada exitosamente. La aplicación se reiniciará para el próximo cajero.", "Cierre", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Restart();
                }
            };

            headerLeft.Controls.Add(lblTitle);
            
            btnAddMovement = new Button { Text = "💸 Retiro Efectivo", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(0, 123, 255), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnAddMovement.FlatAppearance.BorderSize = 0;
            btnAddMovement.Click += (s, e) => {
                var movementForm = new MovementForm(_authClient, _cashRegisterId);
                if (movementForm.ShowDialog(this) == DialogResult.OK)
                {
                    string ticket = "      COMPROBANTE DE RETIRO\n";
                    ticket += new string('-', 42) + "\n";
                    ticket += $"Monto: $ {movementForm.Amount:N2}\n";
                    ticket += $"Motivo: {movementForm.Description}\n";
                    ticket += $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n";
                    ticket += new string('-', 42) + "\n\n\n\n";
                    ticket += "Firma: ___________________________\n\n";
                    ticket += "Aclaracion: ______________________\n\n";
                    
                    PrintTicket(ticket);
                    MessageBox.Show("Retiro registrado exitosamente.", "Egreso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            
            headerLeft.Controls.Add(btnAddMovement);
            headerLeft.Controls.Add(btnCloseRegister);
            headerLeft.Controls.Add(btnSync);
            headerLeft.Controls.Add(lblItemsCount);
            
            gridItems = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = bgColor, BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(40, 42, 54),
                EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, RowTemplate = { Height = 80 }
            };
            gridItems.DefaultCellStyle.BackColor = panelColor; 
            gridItems.DefaultCellStyle.ForeColor = textColor;
            gridItems.DefaultCellStyle.Font = new Font("Segoe UI", 12);
            gridItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 48, 66); 
            gridItems.DefaultCellStyle.SelectionForeColor = textColor;
            gridItems.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            gridItems.ColumnHeadersDefaultCellStyle.BackColor = bgColor; 
            gridItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gray;
            gridItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            gridItems.ColumnHeadersHeight = 45;
            gridItems.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            
            gridItems.Columns.Add("Id", "ID"); 
            gridItems.Columns.Add("Name", "PRODUCTO"); 
            gridItems.Columns.Add("Price", "PRECIO UNIT.");
            
            var btnMinus = new DataGridViewButtonColumn { Name = "btnMinus", HeaderText = "", Text = "-", UseColumnTextForButtonValue = true, Width = 40, FlatStyle = FlatStyle.Flat };
            btnMinus.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66); btnMinus.DefaultCellStyle.ForeColor = Color.White;
            gridItems.Columns.Add(btnMinus);
            
            gridItems.Columns.Add("Quantity", "CANTIDAD"); 
            
            var btnPlus = new DataGridViewButtonColumn { Name = "btnPlus", HeaderText = "", Text = "+", UseColumnTextForButtonValue = true, Width = 40, FlatStyle = FlatStyle.Flat };
            btnPlus.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66); btnPlus.DefaultCellStyle.ForeColor = Color.White;
            gridItems.Columns.Add(btnPlus);
            
            gridItems.Columns.Add("SubTotal", "SUBTOTAL");
            
            gridItems.Columns["Id"].Visible = false; 
            gridItems.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            gridItems.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridItems.Columns["Quantity"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridItems.Columns["SubTotal"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            
            gridItems.Columns["Price"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridItems.Columns["Quantity"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridItems.Columns["SubTotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

            var priceStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White };
            gridItems.Columns["Price"].DefaultCellStyle = priceStyle;
            gridItems.Columns["SubTotal"].DefaultCellStyle = priceStyle;
            gridItems.Columns["Name"].DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            gridItems.Columns["Quantity"].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            
            gridItems.CellContentClick += GridItems_CellContentClick;
            
            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = bgColor };
            gridItems.Margin = new Padding(1);
            gridContainer.Controls.Add(gridItems);
            
            leftPanel.Controls.Add(gridContainer);
            leftPanel.Controls.Add(headerLeft);
            
            Panel rightPanel = new Panel { Dock = DockStyle.Fill };
            
            Panel totalBox = new Panel { Height = 100, Dock = DockStyle.Top, BackColor = Color.FromArgb(10, 10, 15), Margin = new Padding(0, 0, 0, 20) };
            totalBox.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, totalBox.ClientRectangle, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid);
            Label lblTotalText = new Label { Text = "TOTAL A COBRAR", ForeColor = greenColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblTotal = new Label { Text = "$0,00", ForeColor = greenColor, Font = new Font("Segoe UI", 36, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 200, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            totalBox.Controls.Add(lblTotalText); totalBox.Controls.Add(lblTotal);
            
            Panel vueltoBox = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.FromArgb(10, 10, 15), Margin = new Padding(0, 20, 0, 20) };
            Color redColor = Color.FromArgb(239, 68, 68);
            vueltoBox.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, vueltoBox.ClientRectangle, redColor, 1, ButtonBorderStyle.Solid, redColor, 1, ButtonBorderStyle.Solid, redColor, 1, ButtonBorderStyle.Solid, redColor, 1, ButtonBorderStyle.Solid);
            Label lblVueltoText = new Label { Text = "Vuelto / Diferencia:", AutoSize = true, Location = new Point(10, 10) };
            lblVuelto = new Label { Text = "Resta: -$0,00", ForeColor = redColor, Font = new Font("Consolas", 14, FontStyle.Bold), AutoSize = true, Location = new Point(vueltoBox.Width - 150, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            vueltoBox.Controls.Add(lblVueltoText); vueltoBox.Controls.Add(lblVuelto);
            
            FlowLayoutPanel controlsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
            
            GroupBox gbCliente = CreateGroupBox("Cliente (F5 para crear)", 80);
            cmbCustomer = new ComboBox { Width = gbCliente.Width - 110, Location = new Point(10, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12) };
            var btnNewCustomer = new Button { Text = "Nuevo (F5)", Width = 90, Height = 30, Location = new Point(cmbCustomer.Right + 5, 29), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnNewCustomer.FlatAppearance.BorderSize = 0;
            btnNewCustomer.Click += async (s, e) => await CreateNewCustomerDialog();
            gbCliente.Controls.Add(cmbCustomer); 
            gbCliente.Controls.Add(btnNewCustomer);
            controlsPanel.Controls.Add(gbCliente);
            
            GroupBox gbScan = CreateGroupBox("Escanear o Buscar (F1)", 240);
            txtBarcode = new TextBox { Width = gbScan.Width - 90, Location = new Point(10, 30), BackColor = panelColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 16), PlaceholderText = " ⏸ Escanee código o busque por nombre..." };
            lblMultiplier = new Label { Text = "x1", Visible = false, Width = 70, Height = 35, Location = new Point(txtBarcode.Right + 5, 30), BackColor = accentColor, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.TextChanged += TxtBarcode_TextChanged;
            
            lstSearch = new ListBox { Width = gbScan.Width - 20, Height = 140, Location = new Point(10, 75), BackColor = panelColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Visible = false, Font = new Font("Segoe UI", 12) };
            lstSearch.KeyDown += LstSearch_KeyDown;
            lstSearch.DoubleClick += LstSearch_DoubleClick;
            
            gbScan.Controls.Add(txtBarcode); 
            gbScan.Controls.Add(lblMultiplier);
            gbScan.Controls.Add(lstSearch);
            controlsPanel.Controls.Add(gbScan);
            
            GroupBox gbDepts = CreateGroupBox("Venta Rápida por Departamento", 120);
            panelDepartments = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(5, 20, 5, 5), AutoScroll = true };
            gbDepts.Controls.Add(panelDepartments); controlsPanel.Controls.Add(gbDepts);
            
            // Removed gbDesc and gbPago from main UI
            
            Button btnFinalize = new Button { Text = "✔️ FINALIZAR VENTA [F12]", Height = 60, Width = rightPanel.Width, Dock = DockStyle.Bottom, BackColor = accentColor, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnFinalize.FlatAppearance.BorderSize = 0; btnFinalize.Click += BtnFinalize_Click;
            
            rightPanel.Controls.Add(controlsPanel); rightPanel.Controls.Add(vueltoBox); rightPanel.Controls.Add(totalBox); rightPanel.Controls.Add(btnFinalize);
            mainLayout.Controls.Add(leftPanel, 0, 0); mainLayout.Controls.Add(rightPanel, 1, 0);
            this.Controls.Add(mainLayout);
            
            this.Resize += (s, e) => {
                lblItemsCount.Left = headerLeft.Width - lblItemsCount.Width - 10;
                btnSync.Left = lblItemsCount.Left - btnSync.Width - 20;
                btnCloseRegister.Left = btnSync.Left - btnCloseRegister.Width - 20;
                btnAddMovement.Left = btnCloseRegister.Left - btnAddMovement.Width - 20;
                lblTotalText.Left = totalBox.Width - lblTotalText.Width - 10;
                lblTotal.Left = totalBox.Width - lblTotal.Width - 10;
                lblVuelto.Left = vueltoBox.Width - lblVuelto.Width - 10;
                
                foreach(Control c in controlsPanel.Controls) { c.Width = rightPanel.Width - 20; }
                txtBarcode.Width = gbScan.Width - 90;
                lblMultiplier.Left = txtBarcode.Right + 5;
                lstSearch.Width = gbScan.Width - 20;
                
                cmbCustomer.Width = gbCliente.Width - 110;
                btnNewCustomer.Location = new Point(cmbCustomer.Right + 5, 29);
            };
        }

        private GroupBox CreateGroupBox(string title, int height)
        {
            return new GroupBox { Text = title, ForeColor = Color.LightGray, Height = height, Width = 400, Margin = new Padding(0, 0, 0, 15), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        }

        private async void TxtBarcode_TextChanged(object? sender, EventArgs e)
        {
            string input = txtBarcode.Text.TrimStart();
            
            // Extract multiplier and remove it from text
            var match = System.Text.RegularExpressions.Regex.Match(input, @"^(\d+(?:[.,]\d+)?)\s*(?:\*|x|X)\s*(.*)");
            if (match.Success)
            {
                if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pQty))
                {
                    _nextQuantity = pQty;
                    string remainingText = match.Groups[2].Value.TrimStart();
                    
                    // Update UI and prevent recursive parsing issues
                    lblMultiplier.Text = $"x{_nextQuantity:G}";
                    lblMultiplier.Visible = true;
                    
                    txtBarcode.Text = remainingText;
                    txtBarcode.SelectionStart = txtBarcode.Text.Length;
                    return; // Return here, changing Text will re-trigger this event
                }
            }
            else if (input.Contains("*"))
            {
                var parts = input.Split('*');
                if (parts.Length == 2 && decimal.TryParse(parts[0], out decimal pQty)) 
                {
                    _nextQuantity = pQty;
                    string remainingText = parts[1].TrimStart();
                    
                    lblMultiplier.Text = $"x{_nextQuantity:G}";
                    lblMultiplier.Visible = true;
                    
                    txtBarcode.Text = remainingText;
                    txtBarcode.SelectionStart = txtBarcode.Text.Length;
                    return; // Return here, changing Text will re-trigger this event
                }
            }
            
            // If the textbox is cleared explicitly by the user, we keep the multiplier 
            // until an item is added. Or we can reset it? 
            // We'll keep it so they can type `2*` and then type and retype the product.

            string q = input.Trim();
            if (q.Length >= 3)
            {
                using var db = new LocalDbContext();
                string qLower = q.ToLower();
                var results = await db.Products
                    .Where(p => p.Name.ToLower().Contains(qLower) || p.Barcode == q || p.InternalCode.ToString() == q)
                    .Take(10)
                    .ToListAsync();
                
                lstSearch.Items.Clear();
                foreach(var p in results) { lstSearch.Items.Add(new ComboBoxItem(p.Name, p.Id)); }
                lstSearch.Visible = results.Count > 0;
            }
            else
            {
                lstSearch.Visible = false;
            }
        }

        private void LstSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && lstSearch.SelectedItem != null)
            {
                e.SuppressKeyPress = true;
                SelectSearchItem();
            }
        }

        private void LstSearch_DoubleClick(object? sender, EventArgs e)
        {
            if (lstSearch.SelectedItem != null) SelectSearchItem();
        }

        private async void SelectSearchItem()
        {
            var item = lstSearch.SelectedItem as ComboBoxItem;
            if (item != null)
            {
                decimal qtyToAdd = _nextQuantity;
                using var db = new LocalDbContext();
                var p = await db.Products.FindAsync(item.Value);
                if (p != null) AddRow(p.Id, p.Name, p.Price, qtyToAdd, p.Stock);
                
                _nextQuantity = 1;
                lblMultiplier.Visible = false;
                txtBarcode.Clear();
                lstSearch.Visible = false;
                txtBarcode.Focus();
            }
        }

        private class ComboBoxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public ComboBoxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        private async void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && lstSearch.Visible && lstSearch.Items.Count > 0)
            {
                e.SuppressKeyPress = true;
                lstSearch.Focus();
                lstSearch.SelectedIndex = 0;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string input = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(input)) return;
                
                if (lstSearch.Visible && lstSearch.Items.Count > 0)
                {
                    lstSearch.Focus();
                    lstSearch.SelectedIndex = 0;
                    SelectSearchItem();
                    return;
                }

                decimal qtyToAdd = _nextQuantity;
                string code = input;

                _nextQuantity = 1;
                lblMultiplier.Visible = false;
                txtBarcode.Clear();
                lstSearch.Visible = false;
                
                await ProcessBarcodeAsync(code, qtyToAdd);
            }
        }

        private async Task ProcessBarcodeAsync(string barcode, decimal quantity)
        {
            using var db = new LocalDbContext();
            var product = await db.Products.FirstOrDefaultAsync(p => p.Barcode == barcode || p.InternalCode.ToString() == barcode);
            
            if (product != null) AddRow(product.Id, product.Name, product.Price, quantity, product.Stock);
            else MessageBox.Show("Producto no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async Task CreateNewCustomerDialog()
        {
            using var modal = new Form();
            modal.Text = "Nuevo Cliente";
            modal.Size = new Size(400, 420);
            modal.StartPosition = FormStartPosition.CenterParent;
            modal.BackColor = Color.FromArgb(20, 20, 30);
            modal.ForeColor = Color.White;
            modal.FormBorderStyle = FormBorderStyle.FixedDialog;
            modal.MaximizeBox = false;
            modal.MinimizeBox = false;

            var title = new Label { Text = "Crear/Buscar Cliente", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.LightSkyBlue, AutoSize = true, Location = new Point(30, 20) };
            
            var lblDni = new Label { Text = "DNI / CUIT", Location = new Point(30, 60), AutoSize = true };
            var txtDni = new TextBox { Location = new Point(30, 85), Width = 320, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var lblName = new Label { Text = "Nombre Completo", Location = new Point(30, 120), AutoSize = true };
            var txtName = new TextBox { Location = new Point(30, 145), Width = 320, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var lblEmail = new Label { Text = "Email", Location = new Point(30, 180), AutoSize = true };
            var txtEmail = new TextBox { Location = new Point(30, 205), Width = 320, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var lblPhone = new Label { Text = "Teléfono", Location = new Point(30, 240), AutoSize = true };
            var txtPhone = new TextBox { Location = new Point(30, 265), Width = 320, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var btnSave = new Button { Text = "Guardar y Seleccionar (Enter)", Location = new Point(30, 320), Width = 320, Height = 40, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;

            modal.Controls.AddRange(new Control[] { title, lblDni, txtDni, lblName, txtName, lblEmail, txtEmail, lblPhone, txtPhone, btnSave });
            
            txtDni.Leave += async (s, e) => {
                var dni = txtDni.Text.Trim();
                if (string.IsNullOrEmpty(dni)) return;
                using var db = new LocalDbContext();
                var existing = await db.Customers.FirstOrDefaultAsync(c => c.Dni == dni || c.Cuit == dni);
                if (existing != null)
                {
                    txtName.Text = existing.Name;
                    txtEmail.Text = existing.Email;
                    txtPhone.Text = existing.Phone;
                    title.Text = "Cliente Existente";
                    title.ForeColor = Color.YellowGreen;
                }
            };

            btnSave.Click += async (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("El nombre es requerido."); return; }
                using var db = new LocalDbContext();
                var dni = txtDni.Text.Trim();
                var cust = string.IsNullOrEmpty(dni) ? null : await db.Customers.FirstOrDefaultAsync(c => c.Dni == dni || c.Cuit == dni);
                
                if (cust == null)
                {
                    cust = new Customer
                    {
                        Name = txtName.Text.Trim(),
                        Dni = string.IsNullOrEmpty(dni) ? null : dni,
                        Email = txtEmail.Text.Trim(),
                        Phone = txtPhone.Text.Trim(),
                        IsActive = true
                    };
                    db.Customers.Add(cust);
                    db.Entry(cust).Property("IsSynced").CurrentValue = false;
                    await db.SaveChangesAsync();
                }

                // Update combo box
                var customers = await db.Customers.ToListAsync();
                customers.Insert(0, new Customer { Id = 0, Name = "Consumidor Final" });
                cmbCustomer.DataSource = customers;
                cmbCustomer.SelectedValue = cust.Id;
                
                modal.DialogResult = DialogResult.OK;
                modal.Close();
            };

            modal.AcceptButton = btnSave;
            
            modal.ShowDialog();
            txtBarcode.Focus();
        }

        private async Task ProcessDepartmentSale(Department dept)
        {
            using var db = new LocalDbContext();
            var vprod = await db.Products.FirstOrDefaultAsync(p => p.Id == dept.VirtualProductId);
            if (vprod == null)
            {
                MessageBox.Show("Producto virtual del departamento no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var modal = new Form();
            modal.Text = "Venta por Departamento";
            modal.Size = new Size(400, 350);
            modal.StartPosition = FormStartPosition.CenterParent;
            modal.BackColor = Color.FromArgb(20, 20, 30);
            modal.ForeColor = Color.White;
            modal.FormBorderStyle = FormBorderStyle.FixedDialog;
            modal.MaximizeBox = false;
            modal.MinimizeBox = false;

            var title = new Label { Text = "Venta por Departamento", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.MediumPurple, AutoSize = true, Location = new Point(50, 20) };
            var subtitle = new Label { Text = $"Ingresando en {dept.Name}", Font = new Font("Segoe UI", 10), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(90, 60) };
            
            var lblPrice = new Label { Text = "🏷️ Precio a Cobrar ($)", Location = new Point(40, 100), AutoSize = true };
            var txtPrice = new TextBox { Text = "0.00", Location = new Point(40, 125), Width = 300, Font = new Font("Segoe UI", 14), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var lblDesc = new Label { Text = "💬 Descripción", Location = new Point(40, 170), AutoSize = true };
            var txtDesc = new TextBox { Text = dept.Name, Location = new Point(40, 195), Width = 300, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var btnAdd = new Button { Text = "+ Agregar", Location = new Point(80, 250), Width = 100, Height = 40, BackColor = Color.MediumSlateBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button { Text = "x Cancelar", Location = new Point(200, 250), Width = 100, Height = 40, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;

            modal.Controls.Add(title); modal.Controls.Add(subtitle);
            modal.Controls.Add(lblPrice); modal.Controls.Add(txtPrice);
            modal.Controls.Add(lblDesc); modal.Controls.Add(txtDesc);
            modal.Controls.Add(btnAdd); modal.Controls.Add(btnCancel);

            modal.AcceptButton = btnAdd;
            modal.CancelButton = btnCancel;

            btnCancel.Click += (s, e) => modal.DialogResult = DialogResult.Cancel;
            btnAdd.Click += (s, e) => {
                string priceText = txtPrice.Text.Trim().Replace(".", ",");
                if (decimal.TryParse(priceText, out decimal p) && p > 0)
                {
                    modal.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Ingrese un precio válido mayor a 0", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrice.Focus();
                    txtPrice.SelectAll();
                }
            };

            modal.Shown += (s, e) => { txtPrice.Focus(); txtPrice.SelectAll(); };

            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                string priceText = txtPrice.Text.Trim().Replace(".", ",");
                decimal p = Convert.ToDecimal(priceText);
                string desc = string.IsNullOrWhiteSpace(txtDesc.Text) ? dept.Name : txtDesc.Text;
                AddRow(vprod.Id, desc, p, 1);
            }
        }

        private void GridItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            
            var row = gridItems.Rows[e.RowIndex];
            decimal currentQty = Convert.ToDecimal(row.Cells["Quantity"].Value);
            decimal price = Convert.ToDecimal(row.Cells["Price"].Value);

            if (e.ColumnIndex == gridItems.Columns["btnMinus"].Index)
            {
                if (currentQty > 1)
                {
                    row.Cells["Quantity"].Value = currentQty - 1;
                    row.Cells["SubTotal"].Value = (currentQty - 1) * price;
                }
                else
                {
                    gridItems.Rows.RemoveAt(e.RowIndex);
                }
                UpdateTotals();
            }
            else if (e.ColumnIndex == gridItems.Columns["btnPlus"].Index)
            {
                row.Cells["Quantity"].Value = currentQty + 1;
                row.Cells["SubTotal"].Value = (currentQty + 1) * price;
                UpdateTotals();
            }
        }

        private void AddRow(int id, string name, decimal price, decimal qty, decimal stock = 0)
        {
            bool found = false;
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                if ((int)row.Cells["Id"].Value == id && row.Cells["Name"].Value.ToString().StartsWith(name) && Convert.ToDecimal(row.Cells["Price"].Value) == price)
                {
                    decimal currentQty = Convert.ToDecimal(row.Cells["Quantity"].Value);
                    row.Cells["Quantity"].Value = currentQty + qty;
                    row.Cells["SubTotal"].Value = (currentQty + qty) * price;
                    found = true; break;
                }
            }
            if (!found) 
            {
                // We add the product name and a mockup "Stock" text to match the requested design
                string displayString = $"{name} ({stock:0.##})";
                gridItems.Rows.Add(id, displayString, price, "-", qty, "+", price * qty);
            }
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            decimal total = 0; int items = 0;
            foreach (DataGridViewRow row in gridItems.Rows)
            {
                total += Convert.ToDecimal(row.Cells["SubTotal"].Value); items++;
            }
            lblTotal.Text = $"${total:N2}"; 
            lblItemsCount.Text = $"{items} ítems cargados";
            lblVuelto.Text = $"Resta: -${total:N2}";

            // Re-align right-anchored elements after changing their text
            if (lblTotal.Parent != null)
                lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
            if (lblVuelto.Parent != null)
                lblVuelto.Left = lblVuelto.Parent.Width - lblVuelto.Width - 10;
            if (lblItemsCount.Parent != null)
            {
                lblItemsCount.Left = lblItemsCount.Parent.Width - lblItemsCount.Width - 10;
                btnSync.Left = lblItemsCount.Left - btnSync.Width - 20;
            }
        }

        private async void BtnFinalize_Click(object? sender, EventArgs e)
        {
            if (gridItems.Rows.Count == 0) return;
            decimal total = 0;
            var sale = new Sale { 
                GlobalId = Guid.NewGuid(), 
                Date = DateTime.Now, 
                IsSynced = false, 
                CustomerId = cmbCustomer.SelectedValue as int? == 0 ? null : (int?)cmbCustomer.SelectedValue, 
                UserId = _userId, 
                CashRegisterId = _cashRegisterId,
                RequestElectronicInvoice = _requestElectronicInvoice
            };

            foreach (DataGridViewRow row in gridItems.Rows)
            {
                decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
                decimal qty = Convert.ToDecimal(row.Cells["Quantity"].Value);
                total += (price * qty);
                sale.Items.Add(new SaleItem { ProductId = Convert.ToInt32(row.Cells["Id"].Value), UnitPrice = price, Quantity = qty });
            }
            sale.TotalAmount = total; sale.SubTotal = total;

            using var modal = new Form();
            modal.Text = "Finalizar Venta";
            modal.Size = new Size(400, 350);
            modal.StartPosition = FormStartPosition.CenterParent;
            modal.BackColor = Color.FromArgb(20, 20, 30);
            modal.ForeColor = Color.White;
            modal.FormBorderStyle = FormBorderStyle.FixedDialog;
            modal.MaximizeBox = false;
            modal.MinimizeBox = false;

            var title = new Label { Text = "Medio de Pago", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.LightSkyBlue, AutoSize = true, Location = new Point(40, 20) };
            var lblTotalText = new Label { Text = $"Total a cobrar: ${total:N2}", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.YellowGreen, AutoSize = true, Location = new Point(40, 60) };
            
            var lblMethod = new Label { Text = "Medio de Pago", Location = new Point(40, 100), AutoSize = true };
            var localCmbPaymentMethod = new ComboBox { Location = new Point(40, 125), Width = 300, Font = new Font("Segoe UI", 12), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White };
            
            using var db = new LocalDbContext();
            var paymentMethods = await db.PaymentMethods.Where(p => p.IsActive).ToListAsync();
            if (paymentMethods.Count == 0) paymentMethods.Add(new PaymentMethod { Id = 1, Name = "Efectivo" });
            localCmbPaymentMethod.DataSource = paymentMethods;
            localCmbPaymentMethod.DisplayMember = "Name";
            localCmbPaymentMethod.ValueMember = "Id";

            var lblPagaCon = new Label { Text = "Paga con ($)", Location = new Point(40, 170), AutoSize = true };
            var txtPagaCon = new TextBox { Text = total.ToString("0.00"), Location = new Point(40, 195), Width = 140, Font = new Font("Segoe UI", 14), BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var lblVueltoModal = new Label { Text = "Vuelto: $0.00", Location = new Point(200, 195), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.Gold };

            var btnConfirm = new Button { Text = "✔️ CONFIRMAR [Enter]", Location = new Point(40, 250), Width = 300, Height = 40, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            btnConfirm.FlatAppearance.BorderSize = 0;

            modal.Controls.AddRange(new Control[] { title, lblTotalText, lblMethod, localCmbPaymentMethod, lblPagaCon, txtPagaCon, lblVueltoModal, btnConfirm });

            txtPagaCon.TextChanged += (s, ev) => {
                var input = txtPagaCon.Text.Replace(".", ",");
                if (decimal.TryParse(input, out decimal paga)) {
                    var diff = paga - total;
                    lblVueltoModal.Text = diff >= 0 ? $"Vuelto: ${diff:N2}" : $"Falta: ${Math.Abs(diff):N2}";
                    lblVueltoModal.ForeColor = diff >= 0 ? Color.Gold : Color.Tomato;
                }
            };

            btnConfirm.Click += async (s, ev) => {
                sale.Payments.Add(new SalePayment 
                { 
                    PaymentMethodId = (int)localCmbPaymentMethod.SelectedValue, 
                    Amount = total 
                });

                using var context = new LocalDbContext();
                context.Sales.Add(sale); 
                await context.SaveChangesAsync();

                string paymentMethodName = localCmbPaymentMethod.Text;
                string ticketText = GenerateTicketText(sale, paymentMethodName);
                
                // Intentar imprimir el ticket automáticamente
                PrintTicket(ticketText);

                var msgResult = MessageBox.Show($"Venta registrada exitosamente.\nTicket: {sale.GlobalId}\n\n¿Desea imprimir una copia del ticket?", "Caja", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (msgResult == DialogResult.Yes)
                {
                    PrintTicket(ticketText);
                }

                gridItems.Rows.Clear(); 
                UpdateTotals();
                
                modal.DialogResult = DialogResult.OK;
                modal.Close();
                
                _ = Task.Run(async () => {
                    try { await _syncWorker.PerformSyncAsync(); }
                    catch (Exception) { 
                        this.Invoke((MethodInvoker)delegate {
                            btnSync.Text = "⚠️ Error Sync";
                            btnSync.ForeColor = Color.Red;
                        });
                    }
                });
            };

            modal.AcceptButton = btnConfirm;
            
            modal.Shown += (s, ev) => {
                txtPagaCon.Focus();
                txtPagaCon.SelectAll();
            };

            modal.FormClosed += (s, ev) => txtBarcode.Focus();
            modal.ShowDialog();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1) { txtBarcode.Focus(); return true; }
            if (keyData == Keys.F11) { ToggleInvoiceType(); return true; }
            if (keyData == Keys.F12) { BtnFinalize_Click(this, EventArgs.Empty); return true; }
            if (keyData == Keys.F5) { _ = CreateNewCustomerDialog(); return true; }
            
            if (_departmentHotkeys.TryGetValue(keyData, out var dept))
            {
                _ = ProcessDepartmentSale(dept);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void PrintTicket(string text, string printerName = "", int copies = 1)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                if (!string.IsNullOrEmpty(printerName))
                    pd.PrinterSettings.PrinterName = printerName;
                
                pd.PrintPage += (s, e) =>
                {
                    Font printFont = new Font("Courier New", 8, FontStyle.Bold);
                    float yPos = 0;
                    int count = 0;
                    float leftMargin = 0;
                    float topMargin = 0;
                    if (e.Graphics == null) return;
                    
                    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach(var l in lines)
                    {
                        yPos = topMargin + (count * printFont.GetHeight(e.Graphics));
                        e.Graphics.DrawString(l, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
                        count++;
                    }
                };
                for (int i = 0; i < copies; i++) pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al intentar imprimir el ticket: " + ex.Message, "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string GenerateTicketText(Sale sale, string paymentMethodName)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("         TICKET DE VENTA");
            sb.AppendLine("=================================");
            sb.AppendLine($"Fecha: {sale.Date:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Ticket: {sale.GlobalId.ToString().Substring(0,8).ToUpper()}");
            
            string displayName = "Cajero";
            if (!string.IsNullOrEmpty(this.Text) && this.Text.Contains("Cajero: "))
            {
                int idx = this.Text.IndexOf("Cajero: ") + 8;
                int endIdx = this.Text.IndexOf(" -", idx);
                if (endIdx > idx) displayName = this.Text.Substring(idx, endIdx - idx);
            }
            sb.AppendLine($"Cajero: {displayName}");
            
            sb.AppendLine("---------------------------------");
            sb.AppendLine("Cant  Descripcion         Importe");
            foreach(var item in sale.Items)
            {
                using var db = new LocalDbContext();
                var prod = db.Products.FirstOrDefault(p => p.Id == item.ProductId);
                string pName = prod != null ? prod.Name : "Producto";
                if (pName.Length > 18) pName = pName.Substring(0, 18);
                
                sb.AppendLine($"{item.Quantity,4} {pName,-18} ${(item.Quantity * item.UnitPrice),7:0.00}");
            }
            sb.AppendLine("---------------------------------");
            sb.AppendLine($"TOTAL:                  ${sale.TotalAmount,7:0.00}");
            sb.AppendLine($"Medio de Pago: {paymentMethodName}");
            sb.AppendLine("=================================");
            sb.AppendLine("      GRACIAS POR SU COMPRA");
            sb.AppendLine("\n\n\n\n\n\n");
            return sb.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _syncWorker.Stop(); base.OnFormClosing(e);
        }
    }
}
