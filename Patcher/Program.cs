using System;
using System.IO;

class Program
{
    static void Main()
    {
        string path = @""..\src\GestionQ.CajaPOS\Form1.cs"";
        string content = File.ReadAllText(path);
        
        // Add DESCUENTO column to grid
        content = content.Replace(
            @""gridItems.Columns.Add(""""SubTotal"""", """"SUBTOTAL"""");"", 
            @""gridItems.Columns.Add(""""Discount"""", """"DESCUENTO"""");
        gridItems.Columns.Add(""""SubTotal"""", """"SUBTOTAL"""");""
        );

        content = content.Replace(
            @""gridItems.Columns[""""SubTotal""""].Width = 150;"",
            @""gridItems.Columns[""""Discount""""].Width = 120;
        gridItems.Columns[""""SubTotal""""].Width = 150;""
        );

        content = content.Replace(
            @""gridItems.Columns[""""SubTotal""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;"",
            @""gridItems.Columns[""""Discount""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        gridItems.Columns[""""SubTotal""""].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;""
        );

        content = content.Replace(
            @""gridItems.Columns[""""SubTotal""""].DefaultCellStyle = defaultCellStyle;"",
            @""gridItems.Columns[""""Discount""""].DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = """"C2"""", Font = new Font(""""Segoe UI"""", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(248, 113, 113) };
        gridItems.Columns[""""SubTotal""""].DefaultCellStyle = defaultCellStyle;""
        );
        
        // Update AddRow to calculate discount
        string addRowSearch = @""gridItems.Rows.Add(id, text, text, price, """"-"""", qty, """"+"""", price * qty);"";
        string addRowReplace = @""
        decimal grossSubTotal = price * qty;
        decimal subTotal = grossSubTotal;
        decimal discount = 0;

        var promo = _activePromotions.FirstOrDefault(p => p.ProductIds.Contains(id) && p.Type == """"XForY"""");
        if (promo != null)
        {
            int take = promo.BuyQuantity ?? 0;
            int pay = promo.PayQuantity ?? 0;
            if (take > 0 && pay > 0)
            {
                int timesPromoApplied = (int)(qty / take);
                decimal remainder = qty % take;
                subTotal = (timesPromoApplied * pay * price) + (remainder * price);
                discount = grossSubTotal - subTotal;
            }
        }
        gridItems.Rows.Add(id, text, text, price, """"-"""", qty, """"+"""", discount, subTotal);"";
        
        content = content.Replace(addRowSearch, addRowReplace);
        
        File.WriteAllText(path, content);
        Console.WriteLine(""Done replacing AddRow!"");
    }
}
