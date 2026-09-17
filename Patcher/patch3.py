import sys

def patch_file():
    path = r'../src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Grid columns
    content = content.replace(
        'gridItems.Columns.Add("SubTotal", "SUBTOTAL");', 
        'gridItems.Columns.Add("Discount", "DESCUENTO");\n\t\tgridItems.Columns.Add("SubTotal", "SUBTOTAL");'
    )
    content = content.replace(
        'gridItems.Columns["SubTotal"].Width = 150;',
        'gridItems.Columns["Discount"].Width = 120;\n\t\tgridItems.Columns["SubTotal"].Width = 150;'
    )
    content = content.replace(
        'gridItems.Columns["SubTotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;',
        'gridItems.Columns["Discount"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;\n\t\tgridItems.Columns["SubTotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;'
    )
    content = content.replace(
        'gridItems.Columns["SubTotal"].DefaultCellStyle = defaultCellStyle;',
        'gridItems.Columns["Discount"].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(248, 113, 113) };\n\t\tgridItems.Columns["SubTotal"].DefaultCellStyle = defaultCellStyle;'
    )
    
    # 2. AddRow body replacement
    old_add_row = '''	private void AddRow(int id, string originalName, string customName, decimal price, decimal quantityToAdd)
	{
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			if (Convert.ToInt32(item.Cells["Id"].Value) == id && item.Cells["Name"].Value.ToString() == customName)
			{
				decimal num = Convert.ToDecimal(item.Cells["Quantity"].Value) + quantityToAdd;
				item.Cells["Quantity"].Value = num;
				item.Cells["SubTotal"].Value = num * price;
				UpdateTotals();
				return;
			}
		}
		gridItems.Rows.Add(id, originalName, customName, price, "-", quantityToAdd, "+", quantityToAdd * price);
		gridItems.FirstDisplayedScrollingRowIndex = gridItems.RowCount - 1;
		UpdateTotals();
	}'''
    
    new_add_row = '''	private void AddRow(int id, string originalName, string customName, decimal price, decimal quantityToAdd)
	{
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			if (Convert.ToInt32(item.Cells["Id"].Value) == id && item.Cells["Name"].Value.ToString() == customName)
			{
				decimal num = Convert.ToDecimal(item.Cells["Quantity"].Value) + quantityToAdd;
				item.Cells["Quantity"].Value = num;
				
				decimal grossSubTotal = price * num;
				decimal subTotal = grossSubTotal;
				decimal discount = 0;
				var promo = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
				if (promo != null)
				{
					int take = promo.BuyQuantity ?? 0;
					int pay = promo.PayQuantity ?? 0;
					if (take > 0 && pay > 0)
					{
						int timesPromoApplied = (int)(num / take);
						decimal remainder = num % take;
						subTotal = (timesPromoApplied * pay * price) + (remainder * price);
						discount = grossSubTotal - subTotal;
					}
				}
				item.Cells["Discount"].Value = discount;
				item.Cells["SubTotal"].Value = subTotal;
				
				UpdateTotals();
				return;
			}
		}
		
		decimal grossSubTotalNew = price * quantityToAdd;
		decimal subTotalNew = grossSubTotalNew;
		decimal discountNew = 0;
		var promoNew = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
		if (promoNew != null)
		{
			int takeNew = promoNew.BuyQuantity ?? 0;
			int payNew = promoNew.PayQuantity ?? 0;
			if (takeNew > 0 && payNew > 0)
			{
				int timesPromoAppliedNew = (int)(quantityToAdd / takeNew);
				decimal remainderNew = quantityToAdd % takeNew;
				subTotalNew = (timesPromoAppliedNew * payNew * price) + (remainderNew * price);
				discountNew = grossSubTotalNew - subTotalNew;
			}
		}
		
		gridItems.Rows.Add(id, originalName, customName, price, "-", quantityToAdd, "+", discountNew, subTotalNew);
		gridItems.FirstDisplayedScrollingRowIndex = gridItems.RowCount - 1;
		UpdateTotals();
	}'''
    
    if old_add_row in content:
        content = content.replace(old_add_row, new_add_row)
    else:
        print("Could not find AddRow")
        
    # 3. UpdateTotals
    old_update_totals = '''	private void UpdateTotals()
	{
		decimal num = default(decimal);
		int num2 = 0;
		foreach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
		{
			num += Convert.ToDecimal(item.Cells["SubTotal"].Value);
			num2++;
		}
		lblSubTotalValue.Text = "$" + num.ToString("N2");
		lblTotal.Text = "$" + num.ToString("N2");
		if (lblVuelto != null)
		{
			lblVuelto.Text = "Cantidad de Artículos: " + num2;
		}
		if (lblTotal.Parent != null)
		{
			lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
			Label label = lblTotal.Parent.Controls.OfType<Label>().FirstOrDefault((Label c) => c.Text == "TOTAL A COBRAR:");
			if (label != null)
			{
				label.Left = lblTotal.Left - label.Width - 10;
			}
			lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
			lblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
		}
		if (lblItemsCount != null && lblItemsCount.Parent != null)
		{
			lblItemsCount.Left = lblItemsCount.Parent.Width - lblItemsCount.Width - 10;
		}
	}'''
    
    new_update_totals = '''	private void UpdateTotals()
	{
		decimal total = 0m;
		decimal grossTotal = 0m;
		int items = 0;
		foreach (DataGridViewRow row in (IEnumerable)gridItems.Rows)
		{
			grossTotal += Convert.ToDecimal(row.Cells["Price"].Value) * Convert.ToDecimal(row.Cells["Quantity"].Value);
			total += Convert.ToDecimal(row.Cells["SubTotal"].Value);
			items++;
		}
		
		lblSubTotalValue.Text = "$" + grossTotal.ToString("N2");
		lblTotal.Text = "$" + total.ToString("N2");
		
		if (lblVuelto != null)
		{
			lblVuelto.Text = "Cantidad de Artículos: " + items;
		}
		
		decimal discount = grossTotal - total;
		if (discount > 0)
		{
			lblPromoDiscountValue.Text = "-$" + discount.ToString("N2");
			lblPromoDiscountText.Visible = true;
			lblPromoDiscountValue.Visible = true;
		}
		else
		{
			lblPromoDiscountValue.Text = "-.00";
			lblPromoDiscountText.Visible = false;
			lblPromoDiscountValue.Visible = false;
		}
		
		if (lblTotal.Parent != null)
		{
			lblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
			var titleTotal = lblTotal.Parent.Controls.OfType<Label>().FirstOrDefault(c => c.Text == "TOTAL A COBRAR:");
			if (titleTotal != null) titleTotal.Left = lblTotal.Left - titleTotal.Width - 10;
			
			lblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
			lblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
			
			lblPromoDiscountValue.Left = lblTotal.Parent.Width - lblPromoDiscountValue.Width - 10;
			lblPromoDiscountText.Left = lblPromoDiscountValue.Left - lblPromoDiscountText.Width - 10;
		}
		
		if (lblItemsCount != null && lblItemsCount.Parent != null)
		{
			lblItemsCount.Left = lblItemsCount.Parent.Width - lblItemsCount.Width - 10;
		}
	}'''
    
    if old_update_totals in content:
        content = content.replace(old_update_totals, new_update_totals)
    else:
        print("Could not find UpdateTotals")
        
    # 4. GridItems_CellContentClick
    old_click = '''	private void GridItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = gridItems.Rows[e.RowIndex];
		decimal num = Convert.ToDecimal(dataGridViewRow.Cells["Quantity"].Value);
		decimal num2 = Convert.ToDecimal(dataGridViewRow.Cells["Price"].Value);
		if (gridItems.Columns[e.ColumnIndex].Name == "btnMinus")
		{
			num -= 1m;
			if (num <= 0m)
			{
				gridItems.Rows.RemoveAt(e.RowIndex);
			}
			else
			{
				dataGridViewRow.Cells["Quantity"].Value = num;
				dataGridViewRow.Cells["SubTotal"].Value = num * num2;
			}
			txtBarcode.Focus();
		}
		else if (gridItems.Columns[e.ColumnIndex].Name == "btnPlus")
		{
			num += 1m;
			dataGridViewRow.Cells["Quantity"].Value = num;
			dataGridViewRow.Cells["SubTotal"].Value = num * num2;
			txtBarcode.Focus();
		}
		UpdateTotals();
	}'''
    
    new_click = '''	private void GridItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}
		DataGridViewRow dataGridViewRow = gridItems.Rows[e.RowIndex];
		decimal num = Convert.ToDecimal(dataGridViewRow.Cells["Quantity"].Value);
		decimal price = Convert.ToDecimal(dataGridViewRow.Cells["Price"].Value);
		int id = Convert.ToInt32(dataGridViewRow.Cells["Id"].Value);
		
		if (gridItems.Columns[e.ColumnIndex].Name == "btnMinus")
		{
			num -= 1m;
			if (num <= 0m)
			{
				gridItems.Rows.RemoveAt(e.RowIndex);
			}
			else
			{
				dataGridViewRow.Cells["Quantity"].Value = num;
			}
			txtBarcode.Focus();
		}
		else if (gridItems.Columns[e.ColumnIndex].Name == "btnPlus")
		{
			num += 1m;
			dataGridViewRow.Cells["Quantity"].Value = num;
			txtBarcode.Focus();
		}
		else
		{
		    return; // Don't process non-button clicks
		}
		
		if (num > 0)
		{
			decimal grossSubTotal = price * num;
			decimal subTotal = grossSubTotal;
			decimal discount = 0;
			var promo = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
			if (promo != null)
			{
				int take = promo.BuyQuantity ?? 0;
				int pay = promo.PayQuantity ?? 0;
				if (take > 0 && pay > 0)
				{
					int timesPromoApplied = (int)(num / take);
					decimal remainder = num % take;
					subTotal = (timesPromoApplied * pay * price) + (remainder * price);
					discount = grossSubTotal - subTotal;
				}
			}
			dataGridViewRow.Cells["Discount"].Value = discount;
			dataGridViewRow.Cells["SubTotal"].Value = subTotal;
		}
		
		UpdateTotals();
	}'''
    
    if old_click in content:
        content = content.replace(old_click, new_click)
    else:
        print("Could not find GridItems_CellContentClick")
        
    # 5. InitializeUI: add lblPromoDiscountValue/Text to totalBox.Controls!
    add_lbls = '''		totalBox.Controls.Add(lblSubTotalText);
		totalBox.Controls.Add(lblSubTotalValue);
		totalBox.Controls.Add(lblPromoDiscountText);
		totalBox.Controls.Add(lblPromoDiscountValue);
		totalBox.Controls.Add(lblTotalText);'''
    content = content.replace('''		totalBox.Controls.Add(lblSubTotalText);
		totalBox.Controls.Add(lblSubTotalValue);
		totalBox.Controls.Add(lblTotalText);''', add_lbls)

    # Initialize the labels in InitializeUI
    init_lbls = '''		lblSubTotalValue = new Label
		{
			Text = ".00",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 15),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		lblPromoDiscountText = new Label
		{
			Text = "Descuento Promociones:",
			ForeColor = Color.LightSkyBlue,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 150, 35),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right),
			Visible = false
		};
		lblPromoDiscountValue = new Label
		{
			Text = "-.00",
			ForeColor = Color.LightSkyBlue,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 35),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right),
			Visible = false
		};'''
    
    content = content.replace('''		lblSubTotalValue = new Label
		{
			Text = ".00",
			ForeColor = Color.LightGray,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(totalBox.Width - 70, 15),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};''', init_lbls)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print('Patched successfully!')

patch_file()
