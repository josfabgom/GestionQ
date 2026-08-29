import re

with open('src/GestionQ.CajaPOS/Form1.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add class fields
code = re.sub(r'private Label lblVuelto = new\(\);', '''private Label lblVuelto = new();
        private Button btnSettings = new();
        private Label lblSubTotalValue = new();
        private Label lblSubTotalText = new();
        private Label lblPromoDiscountValue = new();
        private Label lblPromoDiscountText = new();''', code)

# 2. totalBox setup
new_totalbox = '''
            Panel totalContainer = new Panel { Dock = DockStyle.Bottom, Height = 210, Padding = new Padding(0, 10, 0, 0) };
            
            Panel totalBox = new Panel { Height = 160, Dock = DockStyle.Top, BackColor = Color.FromArgb(10, 10, 15), Margin = new Padding(0, 0, 0, 10) };
            totalBox.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, totalBox.ClientRectangle, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid, greenColor, 1, ButtonBorderStyle.Solid);
            
            lblSubTotalText = new Label { Text = "SubTotal:", ForeColor = Color.LightGray, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblSubTotalValue = new Label { Text = ",00", ForeColor = Color.LightGray, Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 100, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            lblPromoDiscountText = new Label { Text = "Descuento Promociones:", ForeColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 45), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblPromoDiscountValue = new Label { Text = "-,00", ForeColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 100, 45), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            Label lblTotalText = new Label { Text = "TOTAL A COBRAR", ForeColor = greenColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 150, 80), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblTotal = new Label { Text = ",00", ForeColor = greenColor, Font = new Font("Segoe UI", 36, FontStyle.Bold), AutoSize = true, Location = new Point(totalBox.Width - 200, 95), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            
            totalBox.Controls.Add(lblSubTotalText); totalBox.Controls.Add(lblSubTotalValue);
            totalBox.Controls.Add(lblPromoDiscountText); totalBox.Controls.Add(lblPromoDiscountValue);
            totalBox.Controls.Add(lblTotalText); totalBox.Controls.Add(lblTotal);
            totalContainer.Controls.Add(totalBox);
'''
code = re.sub(r'Panel totalBox.*?totalBox\.Controls\.Add\(lblTotal\);', new_totalbox.strip(), code, flags=re.DOTALL)

# 3. Add to UI
code = re.sub(r'rightPanel\.Controls\.Add\(vueltoBox\);\s*rightPanel\.Controls\.Add\(totalBox\);', '', code)
code = re.sub(r'mainLayout\.Controls\.Add\(leftPanel, 0, 0\);', 'leftPanel.Controls.Add(totalContainer); mainLayout.Controls.Add(leftPanel, 0, 0);', code)

# 4. Remove vueltoBox
code = re.sub(r'Panel vueltoBox.*?vueltoBox\.Controls\.Add\(lblVuelto\);', '', code, flags=re.DOTALL)

# 5. OriginalName column
code = code.replace('gridItems.Columns.Add("Name", "PRODUCTO");', 'gridItems.Columns.Add("OriginalName", "OriginalName"); gridItems.Columns.Add("Name", "PRODUCTO"); gridItems.Columns["OriginalName"].Visible = false;')
code = code.replace('gridItems.Rows.Add(id, displayString, price, "-", qty, "+", price * qty);', 'gridItems.Rows.Add(id, displayString, displayString, price, "-", qty, "+", price * qty);')

# 6. Padding on Name
code = code.replace('gridItems.Columns["Name"].DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 14, FontStyle.Bold) };', 'gridItems.Columns["Name"].DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 14, FontStyle.Bold), WrapMode = DataGridViewTriState.True, Padding = new Padding(5, 10, 5, 10) }; gridItems.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;')

# 7. Add Config Button
config_btn = '''
            btnSettings = new Button { Text = "⚙️ Configurar", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = textColor, Margin = new Padding(10, 0, 0, 0) };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += (s, e) => {
                string currentUrl = AppConfig.ServerUrl;
                string newUrl = Microsoft.VisualBasic.Interaction.InputBox("Ingresa la IP o URL del Servidor Principal (ej: http://192.168.1.50:5144):", "Configuración de Servidor", currentUrl);
                if (!string.IsNullOrWhiteSpace(newUrl) && newUrl != currentUrl)
                {
                    AppConfig.ServerUrl = newUrl;
                    MessageBox.Show("Configuración guardada. La caja se reiniciará para aplicar los cambios.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Restart();
                    Environment.Exit(0);
                }
            };
            btnSync = new Button
'''
code = code.replace('btnSync = new Button', config_btn.strip())

code = code.replace('headerRightPanel.Controls.Add(btnSync);', 'headerRightPanel.Controls.Add(btnSync); headerRightPanel.Controls.Add(btnSettings);')
code = code.replace('btnSync.Left = lblItemsCount.Left - btnSync.Width - 20;', 'btnSync.Left = lblItemsCount.Left - btnSync.Width - 20; btnSettings.Left = btnSync.Left - btnSettings.Width - 20;')
code = code.replace('btnCloseRegister.Left = btnSync.Left - btnCloseRegister.Width - 20;', 'btnCloseRegister.Left = btnSettings.Left - btnCloseRegister.Width - 20;')

# 8. Resize removing vuelto and totalBox text resizing
code = re.sub(r'lblTotalText\.Left = .*?;\s*lblTotal\.Left = .*?;\s*lblVuelto\.Left = .*?;', '', code)

# 9. UpdateTotals Method
new_updatetotals = '''
        private void UpdateTotals()
        {
            decimal rawTotal = 0;
            decimal total = 0; int items = 0;
            
            using var db = new LocalDbContext();
            var activePromos = db.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .ToList();

            foreach (DataGridViewRow row in gridItems.Rows)
            {
                int productId = Convert.ToInt32(row.Cells["Id"].Value);
                decimal qty = Convert.ToDecimal(row.Cells["Quantity"].Value);
                decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
                decimal subTotal = qty * price;
                rawTotal += subTotal;
                string originalName = row.Cells["OriginalName"].Value?.ToString() ?? row.Cells["Name"].Value?.ToString() ?? "";
                
                var promo = activePromos.FirstOrDefault(p => p.ProductIds != null && p.ProductIds.Contains(productId));
                if (promo != null)
                {
                    if (promo.Type == "Percentage")
                    {
                        subTotal = subTotal * (1 - (promo.Value / 100m));
                    }
                    else if (promo.Type == "FixedAmount")
                    {
                        subTotal = Math.Max(0, subTotal - promo.Value);
                    }
                    else if (promo.Type == "XForY" && promo.BuyQuantity > 0 && promo.PayQuantity > 0)
                    {
                        int totalQty = (int)qty;
                        int promoSets = totalQty / promo.BuyQuantity.Value;
                        int remainder = totalQty % promo.BuyQuantity.Value;
                        int paidQty = (promoSets * promo.PayQuantity.Value) + remainder;
                        subTotal = paidQty * price;
                    }
                    else if (promo.Type == "Volume" && promo.BuyQuantity > 0)
                    {
                        int totalQty = (int)qty;
                        int promoSets = totalQty / promo.BuyQuantity.Value;
                        int remainder = totalQty % promo.BuyQuantity.Value;
                        subTotal = (promoSets * promo.Value) + (remainder * price);
                    }
                    
                    if (subTotal < (qty * price))
                    {
                        row.Cells["Name"].Value = originalName + "\\n[ Promo: " + promo.Name.Trim('•', ' ') + " ]";
                    }
                    else
                    {
                        row.Cells["Name"].Value = originalName;
                    }
                }
                else
                {
                    row.Cells["Name"].Value = originalName;
                }
                
                row.Cells["SubTotal"].Value = subTotal;
                total += subTotal;
                items++;
            }
            
            decimal discountTotal = rawTotal - total;
            
            lblSubTotalValue.Text = $"";
            lblPromoDiscountValue.Text = $"-";
            lblTotal.Text = $"";
            lblItemsCount.Text = $"{items} ítems cargados";

            if (lblTotal.Parent != null)
            {
                lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
                lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
                lblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
                lblPromoDiscountValue.Left = lblTotal.Parent.Width - lblPromoDiscountValue.Width - 10;
                lblPromoDiscountText.Left = lblPromoDiscountValue.Left - lblPromoDiscountText.Width - 10;
            }
            if (lblItemsCount.Parent != null)
            {
                lblItemsCount.Left = lblItemsCount.Parent.Width - lblItemsCount.Width - 10;
                btnSync.Left = lblItemsCount.Left - btnSync.Width - 20;
                btnSettings.Left = btnSync.Left - btnSettings.Width - 20;
            }
        }
'''
code = re.sub(r'private void UpdateTotals\(\).*?btnSync\.Left = lblItemsCount\.Left - btnSync\.Width - 20;\s*\}\s*\}', new_updatetotals.strip(), code, flags=re.DOTALL)

with open('src/GestionQ.CajaPOS/Form1.cs', 'w', encoding='utf-8') as f:
    f.write(code)
