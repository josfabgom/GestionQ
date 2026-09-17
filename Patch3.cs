using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @""src\GestionQ.CajaPOS\Form1.cs"";
        string content = File.ReadAllText(path);

        string startToken = ""private void InitializeUI()"";
        string endToken = ""private void GridItems_CellContentClick"";
        
        int start = content.IndexOf(startToken);
        int end = content.IndexOf(endToken);
        
        string before = content.Substring(0, start);
        string after = content.Substring(end);
        
        string newUI = @""private void InitializeUI()
        {
            this.Text = $""""GestionQ - Punto de Venta (Caja) - Cajero: {_currentUser?.FullName ?? """"Desconocido""""} - Caja: {_registerInfo?.Name ?? """"Desconocida""""}"""";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.Font = new Font(""""Segoe UI"""", 10);
            
            Panel headerContainer = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = panelColor };
            
            Label lblTitle = new Label { Text = """"🛒 Punto de Venta (Caja)"""", Font = new Font(""""Segoe UI"""", 16, FontStyle.Bold), AutoSize = true, Location = new Point(20, 15), ForeColor = textColor };
            headerContainer.Controls.Add(lblTitle);
            
            FlowLayoutPanel headerLeft = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 800, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            
            var btnCashWithdrawal = new Button { Text = """"💸 Retiro Efectivo"""", ForeColor = Color.FromArgb(59, 130, 246), FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnCashWithdrawal.FlatAppearance.BorderSize = 0;
            btnCashWithdrawal.Click += (s,e) => {
                var form = new CashWithdrawalForm { Owner = this, StartPosition = FormStartPosition.CenterParent };
                form.ShowDialog();
            };
            
            var btnHistory = new Button { Text = """"📝 Historia / Anular"""", ForeColor = Color.FromArgb(16, 185, 129), FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnHistory.FlatAppearance.BorderSize = 0;
            btnHistory.Click += (s,e) => new SalesHistoryForm().ShowDialog();

            var btnPartialReport = new Button { Text = """"📄 Reporte Parcial"""", ForeColor = Color.FromArgb(245, 158, 11), FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnPartialReport.FlatAppearance.BorderSize = 0;
            btnPartialReport.Click += (s,e) => new PartialReportForm().ShowDialog();

            var btnCloseBox = new Button { Text = """"🔒 Cerrar Caja"""", ForeColor = Color.FromArgb(239, 68, 68), FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnCloseBox.FlatAppearance.BorderSize = 0;
            btnCloseBox.Click += async (s, e) => {
                if (MessageBox.Show(""""¿Desea cerrar la caja del día? Esta acción no se puede deshacer y bloqueará la emisión de nuevas facturas hasta la próxima apertura."""", """"Cerrar Caja"""", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    await CerrarCajaAsync();
                }
            };
            
            btnSettings = new Button { Text = """"⚙️ Configurar"""", ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += (s, e) => {
                var configForm = new Form { Text = """"Configuración"""", Width = 400, Height = 300, StartPosition = FormStartPosition.CenterParent, BackColor = panelColor, ForeColor = textColor };
                configForm.ShowDialog();
            };
            
            btnSync = new Button { Text = """"Sincronizado ✔"""", ForeColor = Color.FromArgb(16, 185, 129), FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 9), AutoSize = true, Margin = new Padding(10, 15, 10, 15), BackColor = panelColor }; btnSync.FlatAppearance.BorderSize = 0;
            
            headerLeft.Controls.Add(btnCashWithdrawal);
            headerLeft.Controls.Add(btnHistory);
            headerLeft.Controls.Add(btnPartialReport);
            headerLeft.Controls.Add(btnCloseBox);
            headerLeft.Controls.Add(btnSettings);
            headerLeft.Controls.Add(btnSync);
            headerContainer.Controls.Add(headerLeft);
            this.Controls.Add(headerContainer);
            
            Panel mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            this.Controls.Add(mainPanel);
            
            SplitContainer splitContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = (int)(this.Width * 0.65), IsSplitterFixed = true };
            mainPanel.Controls.Add(splitContainer);
            
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
            splitContainer.Panel1.Controls.Add(leftPanel);
            
            gridItems = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = panelColor, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(55, 65, 81), RowTemplate = { Height = 50 } };
            gridItems.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(17, 24, 39), ForeColor = Color.Gray, Font = new Font(""""Segoe UI"""", 9, FontStyle.Bold), SelectionBackColor = Color.FromArgb(17, 24, 39) };
            gridItems.EnableHeadersVisualStyles = false;
            gridItems.DefaultCellStyle = new DataGridViewCellStyle { BackColor = panelColor, ForeColor = textColor, SelectionBackColor = Color.FromArgb(55, 65, 81), SelectionForeColor = textColor };
            
            gridItems.Columns.Add(""""Id"""", """"ID""""); 
            gridItems.Columns.Add(""""OriginalName"""", """"OriginalName""""); 
            gridItems.Columns.Add(""""Name"""", """"PRODUCTO""""); 
            gridItems.Columns[""""OriginalName""""].Visible = false; 
            gridItems.Columns.Add(""""Price"""", """"PRECIO UNIT."""" );
            
            var btnMinus = new DataGridViewButtonColumn { Name = """"btnMinus"""", HeaderText = """""""", Text = """"-"""", UseColumnTextForButtonValue = true, Width = 30, FlatStyle = FlatStyle.Flat };
            btnMinus.DefaultCellStyle.BackColor = Color.FromArgb(59, 130, 246); btnMinus.DefaultCellStyle.ForeColor = Color.White;
            gridItems.Columns.Add(btnMinus);
            
            gridItems.Columns.Add(""""Quantity"""", """"CANTIDAD""""); 
            
            var btnPlus = new DataGridViewButtonColumn { Name = """"btnPlus"""", HeaderText = """""""", Text = """"+"""", UseColumnTextForButtonValue = true, Width = 30, FlatStyle = FlatStyle.Flat };
            btnPlus.DefaultCellStyle.BackColor = Color.FromArgb(59, 130, 246); btnPlus.DefaultCellStyle.ForeColor = Color.White;
            gridItems.Columns.Add(btnPlus);
            
            gridItems.Columns.Add(""""Discount"""", """"DESCUENTO"""");
            gridItems.Columns.Add(""""SubTotal"""", """"SUBTOTAL"""");
            
            gridItems.Columns[""""Id""""].Visible = false; 
            gridItems.Columns[""""Name""""].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            gridItems.Columns[""""Price""""].Width = 140;
            gridItems.Columns[""""Quantity""""].Width = 100;
            gridItems.Columns[""""Discount""""].Width = 120;
            gridItems.Columns[""""SubTotal""""].Width = 150;
            
            gridItems.Columns[""""Price""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridItems.Columns[""""Quantity""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridItems.Columns[""""Discount""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridItems.Columns[""""SubTotal""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

            var priceStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = """"C2"""", Font = new Font(""""Segoe UI"""", 12, FontStyle.Bold), ForeColor = Color.White };
            gridItems.Columns[""""Price""""].DefaultCellStyle = priceStyle;
            gridItems.Columns[""""Discount""""].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = """"C2"""", Font = new Font(""""Segoe UI"""", 12, FontStyle.Bold), ForeColor = Color.FromArgb(248, 113, 113) };
            gridItems.Columns[""""SubTotal""""].DefaultCellStyle = priceStyle;
            gridItems.Columns[""""Name""""].DefaultCellStyle = new DataGridViewCellStyle { Font = new Font(""""Segoe UI"""", 14, FontStyle.Bold), WrapMode = DataGridViewTriState.True, Padding = new Padding(5, 10, 5, 10) }; gridItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            gridItems.Columns[""""Quantity""""].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font(""""Segoe UI"""", 14, FontStyle.Bold) };
            
            gridItems.CellContentClick += GridItems_CellContentClick;
            
            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = bgColor };
            gridItems.Margin = new Padding(1);
            gridContainer.Controls.Add(gridItems);
            
            leftPanel.Controls.Add(gridContainer);
            
            Panel totalContainer = new Panel { Dock = DockStyle.Bottom, Height = 140, Padding = new Padding(0, 10, 0, 0) };
            
            Panel totalBox = new Panel { Height = 120, Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 10, 15) };
            totalBox.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, totalBox.ClientRectangle, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid);
            
            lblItemsCount = new Label { Text = """"Cantidad de Articulos: 0"""", ForeColor = Color.Magenta, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(10, totalBox.Height - 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            
            lblSubTotalText = new Label { Text = """"SubTotal:"""", ForeColor = Color.LightGray, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblSubTotalValue = new Label { Text = """".00"""", ForeColor = Color.LightGray, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 60, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            lblPromoDiscountText = new Label { Text = """"Descuento Promociones:"""", ForeColor = Color.LightSkyBlue, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 45), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblPromoDiscountValue = new Label { Text = """".00"""", ForeColor = Color.LightSkyBlue, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 60, 45), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            Label lblTotalText = new Label { Text = """"TOTAL A COBRAR:"""", ForeColor = greenColor, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 300, 80), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            lblTotal = new Label { Text = """".00"""", ForeColor = greenColor, Font = new Font(""""Segoe UI"""", 32, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 180, 60), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            
            totalBox.Controls.Add(lblItemsCount);
            totalBox.Controls.Add(lblSubTotalText); totalBox.Controls.Add(lblSubTotalValue);
            totalBox.Controls.Add(lblPromoDiscountText); totalBox.Controls.Add(lblPromoDiscountValue);
            totalBox.Controls.Add(lblTotalText); totalBox.Controls.Add(lblTotal);
            totalContainer.Controls.Add(totalBox);
            
            leftPanel.Controls.Add(totalContainer);
            
            Panel rightPanel = new Panel { Dock = DockStyle.Fill };
            splitContainer.Panel2.Controls.Add(rightPanel);
            
            FlowLayoutPanel controlsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10, 0, 0, 0) };
            
            // Central Logo inside Right Panel
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, """"images"""", """"logo.png"""");
            PictureBox mainLogo = new PictureBox { Width = rightPanel.Width - 20, Height = 60, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 10, 0, 20) };
            if (File.Exists(logoPath)) mainLogo.Image = Image.FromFile(logoPath);
            controlsPanel.Controls.Add(mainLogo);
            
            GroupBox gbCliente = CreateGroupBox(""""Cliente (F5 para crear)"""", 80);
            cmbCustomer = new ComboBox { Width = gbCliente.Width - 110, Location = new Point(10, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 12) };
            var btnNewCustomer = new Button { Text = """"Nuevo (F5)"""", Width = 90, Height = 30, Location = new Point(cmbCustomer.Right + 5, 29), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 10, FontStyle.Bold) };
            btnNewCustomer.FlatAppearance.BorderSize = 0;
            btnNewCustomer.Click += async (s, e) => await CreateNewCustomerDialog();
            gbCliente.Controls.Add(cmbCustomer); 
            gbCliente.Controls.Add(btnNewCustomer);
            controlsPanel.Controls.Add(gbCliente);
            
            GroupBox gbPromociones = CreateGroupBox(""""Promociones Activas"""", 80);
            lblPromoStatus.Location = new Point(10, 30);
            gbPromociones.Controls.Add(lblPromoStatus);
            controlsPanel.Controls.Add(gbPromociones);

            TableLayoutPanel midPanel = new TableLayoutPanel { Width = rightPanel.Width - 20, Height = 140, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            GroupBox gbScan = CreateGroupBox(""""Escanear o Buscar (F1)"""", 130);
            gbScan.Dock = DockStyle.Fill; gbScan.Margin = new Padding(0, 0, 5, 0);
            
            txtBarcode = new TextBox { Width = gbScan.Width - 20, Location = new Point(10, 30), BackColor = panelColor, ForeColor = textColor, Font = new Font(""""Segoe UI"""", 16) };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            
            lblMultiplier.Text = """"x1""""; lblMultiplier.ForeColor = Color.LightGray; lblMultiplier.Font = new Font(""""Segoe UI"""", 12, FontStyle.Bold); lblMultiplier.AutoSize = true;
            lblMultiplier.Location = new Point(txtBarcode.Right + 5, 35);
            
            lstSearch = new ListBox { Width = gbScan.Width - 20, Height = 60, Location = new Point(10, txtBarcode.Bottom + 5), BackColor = panelColor, ForeColor = textColor, Font = new Font(""""Segoe UI"""", 12), Visible = false };
            lstSearch.DoubleClick += LstSearch_DoubleClick;
            lstSearch.KeyDown += LstSearch_KeyDown;
            
            gbScan.Controls.Add(txtBarcode);
            gbScan.Controls.Add(lblMultiplier);
            gbScan.Controls.Add(lstSearch);
            midPanel.Controls.Add(gbScan, 0, 0);

            GroupBox gbDepts = CreateGroupBox(""""Venta Rápida por Departamento"""", 130);
            gbDepts.Dock = DockStyle.Fill; gbDepts.Margin = new Padding(5, 0, 0, 0);
            
            panelDepartments = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(5) };
            gbDepts.Controls.Add(panelDepartments);
            midPanel.Controls.Add(gbDepts, 1, 0);
            
            controlsPanel.Controls.Add(midPanel);
            
            GroupBox gbImage = CreateGroupBox(""""Imagen del Artículo"""", 260);
            var picArticle = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(10, 10, 15) };
            if (File.Exists(logoPath)) picArticle.Image = Image.FromFile(logoPath);
            gbImage.Controls.Add(picArticle);
            controlsPanel.Controls.Add(gbImage);
            
            rightPanel.Controls.Add(controlsPanel);
            
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = accentColor };
            Button btnCheckout = new Button { Text = """"✔ FINALIZAR VENTA [F12]"""", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Font = new Font(""""Segoe UI"""", 14, FontStyle.Bold), ForeColor = Color.White };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += async (s, e) => await ProcessCheckoutAsync();
            bottomPanel.Controls.Add(btnCheckout);
            rightPanel.Controls.Add(bottomPanel);
            
            this.Resize += (s, e) => {
                if (mainLogo.Parent != null) mainLogo.Width = mainLogo.Parent.Width - 20;
                gbCliente.Width = rightPanel.Width - 20;
                cmbCustomer.Width = gbCliente.Width - 110;
                btnNewCustomer.Location = new Point(cmbCustomer.Right + 5, 29);
                gbPromociones.Width = rightPanel.Width - 20;
                midPanel.Width = rightPanel.Width - 20;
                gbImage.Width = rightPanel.Width - 20;
                
                if (lblTotal.Parent != null) {
                    lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
                    var titleTotal = lblTotal.Parent.Controls.OfType<Label>().FirstOrDefault(c => c.Text == """"TOTAL A COBRAR:"""");
                    if (titleTotal != null) titleTotal.Left = lblTotal.Left - titleTotal.Width - 10;
                    
                    lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
                    lblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
                    
                    lblPromoDiscountValue.Left = lblTotal.Parent.Width - lblPromoDiscountValue.Width - 10;
                    lblPromoDiscountText.Left = lblPromoDiscountValue.Left - lblPromoDiscountText.Width - 10;
                }
            };
        }
"";

        File.WriteAllText(path, before + newUI + after);
    }
}
