using System;
using System.Drawing;
using System.Windows.Forms;
namespace TestGrid {
    class Program {
        [STAThread]
        static void Main() {
            var gridItems = new DataGridView();
            gridItems.Columns.Add(""Id"", ""ID""); 
            gridItems.Columns.Add(""OriginalName"", ""OriginalName""); gridItems.Columns.Add(""Name"", ""PRODUCTO""); 
            gridItems.Columns[""OriginalName""].Visible = false; 
            gridItems.Columns.Add(""Price"", ""PRECIO UNIT."");
            
            var btnMinus = new DataGridViewButtonColumn { Name = ""btnMinus"", HeaderText = """", Text = ""-"", UseColumnTextForButtonValue = true, Width = 45, FlatStyle = FlatStyle.Flat };
            gridItems.Columns.Add(btnMinus);
            
            gridItems.Columns.Add(""Quantity"", ""CANTIDAD""); 
            
            var btnPlus = new DataGridViewButtonColumn { Name = ""btnPlus"", HeaderText = """", Text = ""+"", UseColumnTextForButtonValue = true, Width = 45, FlatStyle = FlatStyle.Flat };
            gridItems.Columns.Add(btnPlus);
            
            gridItems.Columns.Add(""Discount"", ""DESCUENTO"");
            gridItems.Columns.Add(""SubTotal"", ""SUBTOTAL"");
            
            foreach (DataGridViewColumn col in gridItems.Columns) {
                Console.WriteLine($""{col.Name}: {col.HeaderText} - Visible: {col.Visible} - Width: {col.Width}"");
            }
        }
    }
}
