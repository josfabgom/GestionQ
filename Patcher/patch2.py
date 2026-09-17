import sys

def patch_file():
    path = r'../src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Add DESCUENTO column to grid
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
    
    # Replace AddRow logic
    # In decompiled code, AddRow looks like: 
    # gridItems.Rows.Add(vprod.Id, vprod.Name, text, price, "-", num, "+", num * price);
    # Actually let's just find private void AddRow and replace the whole method!

    start_idx = content.find('private void AddRow')
    if start_idx != -1:
        end_idx = content.find('private void UpdateTotals', start_idx)
        if end_idx != -1:
            add_row_method = '''private void AddRow(int id, string originalName, string customName, decimal price, decimal quantityToAdd)
\t{
\t\tforeach (DataGridViewRow item in (IEnumerable)gridItems.Rows)
\t\t{
\t\t\tif (Convert.ToInt32(item.Cells["Id"].Value) == id && item.Cells["Name"].Value.ToString() == customName)
\t\t\t{
\t\t\t\tdecimal num = Convert.ToDecimal(item.Cells["Quantity"].Value) + quantityToAdd;
\t\t\t\titem.Cells["Quantity"].Value = num;
\t\t\t\t
\t\t\t\tdecimal grossSubTotal = price * num;
\t\t\t\tdecimal subTotal = grossSubTotal;
\t\t\t\tdecimal discount = 0;
\t\t\t\tvar promo = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
\t\t\t\tif (promo != null)
\t\t\t\t{
\t\t\t\t\tint take = promo.BuyQuantity ?? 0;
\t\t\t\t\tint pay = promo.PayQuantity ?? 0;
\t\t\t\t\tif (take > 0 && pay > 0)
\t\t\t\t\t{
\t\t\t\t\t\tint timesPromoApplied = (int)(num / take);
\t\t\t\t\t\tdecimal remainder = num % take;
\t\t\t\t\t\tsubTotal = (timesPromoApplied * pay * price) + (remainder * price);
\t\t\t\t\t\tdiscount = grossSubTotal - subTotal;
\t\t\t\t\t}
\t\t\t\t}
\t\t\t\t
\t\t\t\titem.Cells["Discount"].Value = discount;
\t\t\t\titem.Cells["SubTotal"].Value = subTotal;
\t\t\t\tUpdateTotals();
\t\t\t\treturn;
\t\t\t}
\t\t}
\t\t
\t\tdecimal grossSubTotalNew = price * quantityToAdd;
\t\tdecimal subTotalNew = grossSubTotalNew;
\t\tdecimal discountNew = 0;
\t\tvar promoNew = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
\t\tif (promoNew != null)
\t\t{
\t\t\tint takeNew = promoNew.BuyQuantity ?? 0;
\t\t\tint payNew = promoNew.PayQuantity ?? 0;
\t\t\tif (takeNew > 0 && payNew > 0)
\t\t\t{
\t\t\t\tint timesPromoAppliedNew = (int)(quantityToAdd / takeNew);
\t\t\t\tdecimal remainderNew = quantityToAdd % takeNew;
\t\t\t\tsubTotalNew = (timesPromoAppliedNew * payNew * price) + (remainderNew * price);
\t\t\t\tdiscountNew = grossSubTotalNew - subTotalNew;
\t\t\t}
\t\t}
\t\t
\t\tgridItems.Rows.Add(id, originalName, customName, price, "-", quantityToAdd, "+", discountNew, subTotalNew);
\t\tgridItems.FirstDisplayedScrollingRowIndex = gridItems.RowCount - 1;
\t\tUpdateTotals();
\t}

\t'''
            content = content[:start_idx] + add_row_method + content[end_idx:]
    
    # Replace UpdateTotals
    start_idx = content.find('private void UpdateTotals')
    if start_idx != -1:
        end_idx = content.find('private void PrintTicket', start_idx)
        if end_idx != -1:
            update_totals_method = '''private void UpdateTotals()
\t{
\t\tdecimal total = 0m;
\t\tdecimal grossTotal = 0m;
\t\tint items = 0;
\t\tforeach (DataGridViewRow row in (IEnumerable)gridItems.Rows)
\t\t{
\t\t\tgrossTotal += Convert.ToDecimal(row.Cells["Price"].Value) * Convert.ToDecimal(row.Cells["Quantity"].Value);
\t\t\ttotal += Convert.ToDecimal(row.Cells["SubTotal"].Value);
\t\t\titems++;
\t\t}
\t\t
\t\tlblSubTotalValue.Text = "$" + grossTotal.ToString("N2");
\t\tlblTotal.Text = "$" + total.ToString("N2");
\t\t
\t\tif (lblVuelto != null)
\t\t{
\t\t\tlblVuelto.Text = "Cantidad de Artículos: " + items;
\t\t}
\t\t
\t\tdecimal discount = grossTotal - total;
\t\tif (discount > 0)
\t\t{
\t\t\tlblPromoDiscountValue.Text = "-$" + discount.ToString("N2");
\t\t\tlblPromoDiscountText.Visible = true;
\t\t\tlblPromoDiscountValue.Visible = true;
\t\t}
\t\telse
\t\t{
\t\t\tlblPromoDiscountValue.Text = "-.00";
\t\t\tlblPromoDiscountText.Visible = false;
\t\t\tlblPromoDiscountValue.Visible = false;
\t\t}
\t\t
\t\tif (lblTotal.Parent != null)
\t\t{
\t\t\tlblTotal.Left = lblTotal.Parent.Width - lblTotal.Width - 10;
\t\t\tvar titleTotal = lblTotal.Parent.Controls.OfType<Label>().FirstOrDefault(c => c.Text == "TOTAL A COBRAR:");
\t\t\tif (titleTotal != null) titleTotal.Left = lblTotal.Left - titleTotal.Width - 10;
\t\t\t
\t\t\tlblSubTotalValue.Left = lblTotal.Parent.Width - lblSubTotalValue.Width - 10;
\t\t\tlblSubTotalText.Left = lblSubTotalValue.Left - lblSubTotalText.Width - 10;
\t\t\t
\t\t\tlblPromoDiscountValue.Left = lblTotal.Parent.Width - lblPromoDiscountValue.Width - 10;
\t\t\tlblPromoDiscountText.Left = lblPromoDiscountValue.Left - lblPromoDiscountText.Width - 10;
\t\t}
\t\t
\t\tif (lblItemsCount != null && lblItemsCount.Parent != null)
\t\t{
\t\t\tlblItemsCount.Left = lblItemsCount.Parent.Width - lblItemsCount.Width - 10;
\t\t}
\t}

\t'''
            content = content[:start_idx] + update_totals_method + content[end_idx:]

    # Also GridItems_CellContentClick needs to be updated because the column indices or names might be used!
    start_idx = content.find('private void GridItems_CellContentClick')
    if start_idx != -1:
        end_idx = content.find('private void AddRow', start_idx)
        if end_idx != -1:
            click_method = '''private void GridItems_CellContentClick(object? sender, DataGridViewCellEventArgs e)
\t{
\t\tif (e.RowIndex < 0)
\t\t{
\t\t\treturn;
\t\t}
\t\tDataGridViewRow dataGridViewRow = gridItems.Rows[e.RowIndex];
\t\tdecimal num = Convert.ToDecimal(dataGridViewRow.Cells["Quantity"].Value);
\t\tdecimal price = Convert.ToDecimal(dataGridViewRow.Cells["Price"].Value);
\t\tint id = Convert.ToInt32(dataGridViewRow.Cells["Id"].Value);
\t\tif (gridItems.Columns[e.ColumnIndex].Name == "btnMinus")
\t\t{
\t\t\tnum -= 1m;
\t\t\tif (num <= 0m)
\t\t\t{
\t\t\t\tgridItems.Rows.RemoveAt(e.RowIndex);
\t\t\t}
\t\t\telse
\t\t\t{
\t\t\t\tdataGridViewRow.Cells["Quantity"].Value = num;
\t\t\t}
\t\t\ttxtBarcode.Focus();
\t\t}
\t\telse if (gridItems.Columns[e.ColumnIndex].Name == "btnPlus")
\t\t{
\t\t\tnum += 1m;
\t\t\tdataGridViewRow.Cells["Quantity"].Value = num;
\t\t\ttxtBarcode.Focus();
\t\t}
\t\telse
\t\t{
\t\t\treturn; // Not a button click
\t\t}
\t\t
\t\tif (num > 0)
\t\t{
\t\t\tdecimal grossSubTotal = price * num;
\t\t\tdecimal subTotal = grossSubTotal;
\t\t\tdecimal discount = 0;
\t\t\tvar promo = _activePromotions.FirstOrDefault((PromotionSyncDto p) => p.ProductIds.Contains(id) && p.Type == "XForY");
\t\t\tif (promo != null)
\t\t\t{
\t\t\t\tint take = promo.BuyQuantity ?? 0;
\t\t\t\tint pay = promo.PayQuantity ?? 0;
\t\t\t\tif (take > 0 && pay > 0)
\t\t\t\t{
\t\t\t\t\tint timesPromoApplied = (int)(num / take);
\t\t\t\t\tdecimal remainder = num % take;
\t\t\t\t\tsubTotal = (timesPromoApplied * pay * price) + (remainder * price);
\t\t\t\t\tdiscount = grossSubTotal - subTotal;
\t\t\t\t}
\t\t\t}
\t\t\tdataGridViewRow.Cells["Discount"].Value = discount;
\t\t\tdataGridViewRow.Cells["SubTotal"].Value = subTotal;
\t\t}
\t\t
\t\tUpdateTotals();
\t}

\t'''
            content = content[:start_idx] + click_method + content[end_idx:]
            
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print('Patched successfully!')

patch_file()
