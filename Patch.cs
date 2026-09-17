using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace Patch {
    class Program {
        static void Main() {
            string file = @"src\GestionQ.CajaPOS\Form1.cs";
            string content = File.ReadAllText(file);
            
            // Fix Grid columns colors
            content = content.Replace("btnMinus.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66);", "btnMinus.DefaultCellStyle.BackColor = Color.FromArgb(59, 130, 246);");
            content = content.Replace("btnPlus.DefaultCellStyle.BackColor = Color.FromArgb(45, 48, 66);", "btnPlus.DefaultCellStyle.BackColor = Color.FromArgb(59, 130, 246);");

            // Fix controlsPanel layout
            string pattern = @"FlowLayoutPanel controlsPanel = new FlowLayoutPanel.*?foreach\(Control c in controlsPanel\.Controls\) \{ c\.Width = rightPanel\.Width - 20; \}";
            
            string replacement = @"FlowLayoutPanel controlsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
            
            GroupBox gbCliente = CreateGroupBox(""Cliente (F5 para crear)"", 80);
            cmbCustomer = new ComboBox { Width = gbCliente.Width - 110, Location = new Point(10, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = panelColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat, Font = new Font(""Segoe UI"", 12) };
            var btnNewCustomer = new Button { Text = ""Nuevo (F5)"", Width = 90, Height = 30, Location = new Point(cmbCustomer.Right + 5, 29), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font(""Segoe UI"", 10, FontStyle.Bold) };
            btnNewCustomer.FlatAppearance.BorderSize = 0;
            btnNewCustomer.Click += async (s, e) => await CreateNewCustomerDialog();
            gbCliente.Controls.Add(cmbCustomer); 
            gbCliente.Controls.Add(btnNewCustomer);
            controlsPanel.Controls.Add(gbCliente);
            
            GroupBox gbPromociones = CreateGroupBox(""Promociones Activas"", 80);
            lblPromoStatus.Location = new Point(10, 30);
            gbPromociones.Controls.Add(lblPromoStatus);
            controlsPanel.Controls.Add(gbPromociones);

            TableLayoutPanel midPanel = new TableLayoutPanel { Width = rightPanel.Width - 20, Height = 140, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            GroupBox gbScan = CreateGroupBox(""Escanear o Buscar (F1)"", 130);
            gbScan.Dock = DockStyle.Fill; gbScan.Margin = new Padding(0,0,5,0);
            txtBarcode = new TextBox { Width = gbScan.Width - 20, Location = new Point(10, 30), BackColor = panelColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = new Font(""Segoe UI"", 16), PlaceholderText = "" ⏸ Escanee código..."" };
            lblMultiplier = new Label { Text = ""x1"", Visible = false, Width = 70, Height = 35, Location = new Point(txtBarcode.Right - 70, 30), BackColor = accentColor, ForeColor = Color.White, Font = new Font(""Segoe UI"", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.TextChanged += TxtBarcode_TextChanged;
            
            lstSearch = new ListBox { Width = gbScan.Width - 20, Height = 80, Location = new Point(10, 75), BackColor = panelColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Visible = false, Font = new Font(""Segoe UI"", 12) };
            lstSearch.KeyDown += LstSearch_KeyDown;
            lstSearch.DoubleClick += LstSearch_DoubleClick;
            
            gbScan.Controls.Add(txtBarcode); 
            gbScan.Controls.Add(lblMultiplier);
            gbScan.Controls.Add(lstSearch);
            midPanel.Controls.Add(gbScan, 0, 0);

            GroupBox gbDepts = CreateGroupBox(""Venta Rápida por Departamento"", 130);
            gbDepts.Dock = DockStyle.Fill; gbDepts.Margin = new Padding(5,0,0,0);
            panelDepartments = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(5, 20, 5, 5), AutoScroll = true };
            gbDepts.Controls.Add(panelDepartments); 
            midPanel.Controls.Add(gbDepts, 1, 0);
            
            controlsPanel.Controls.Add(midPanel);

            GroupBox gbImage = CreateGroupBox(""Imagen del Artículo"", 200);
            picArticle = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Padding = new Padding(10) };
            try { picArticle.Image = Image.FromFile(""icon.png""); } catch { }
            gbImage.Controls.Add(picArticle);
            controlsPanel.Controls.Add(gbImage);

            Button btnFinalize = new Button { Text = ""✔️ FINALIZAR VENTA [F12]"", Height = 60, Width = rightPanel.Width, Dock = DockStyle.Bottom, BackColor = accentColor, ForeColor = Color.White, Font = new Font(""Segoe UI"", 14, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnFinalize.FlatAppearance.BorderSize = 0; btnFinalize.Click += BtnFinalize_Click;
            
            rightPanel.Controls.Add(controlsPanel);  rightPanel.Controls.Add(btnFinalize);
            leftPanel.Controls.Add(totalContainer); mainLayout.Controls.Add(leftPanel, 0, 0); mainLayout.Controls.Add(rightPanel, 1, 0);
            this.Controls.Add(mainLayout);
            
            this.Resize += (s, e) => {
                lblItemsCount.Left = headerLeft.Width - lblItemsCount.Width - 10;
                btnSync.Left = lblItemsCount.Left - btnSync.Width - 20; btnSettings.Left = btnSync.Left - btnSettings.Width - 20;
                btnCloseRegister.Left = btnSettings.Left - btnCloseRegister.Width - 20;
                btnAddMovement.Left = btnCloseRegister.Left - btnAddMovement.Width - 20;
                
                gbCliente.Width = rightPanel.Width - 20;
                cmbCustomer.Width = gbCliente.Width - 110;
                btnNewCustomer.Left = cmbCustomer.Right + 5;
                
                gbPromociones.Width = rightPanel.Width - 20;
                midPanel.Width = rightPanel.Width - 20;
                gbImage.Width = rightPanel.Width - 20;

                txtBarcode.Width = gbScan.Width - 20;
                lblMultiplier.Left = txtBarcode.Right - 70;
                lstSearch.Width = gbScan.Width - 20;
            }";
            
            content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
            File.WriteAllText(file, content);
        }
    }
}
