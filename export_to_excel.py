import sqlite3
from openpyxl import Workbook

def export_products_excel():
    try:
        conn = sqlite3.connect('gestionq.db')
        cursor = conn.cursor()
        
        # Select all from Products table
        cursor.execute('SELECT * FROM Products')
        
        # Get column names
        columns = [description[0] for description in cursor.description]
        
        # Fetch all rows
        rows = cursor.fetchall()
        
        # Create a new workbook and select active sheet
        wb = Workbook()
        ws = wb.active
        ws.title = "Productos"
        
        # Write headers
        ws.append(columns)
        
        # Write data rows
        for row in rows:
            ws.append(row)
            
        # Save workbook
        excel_filename = 'productos.xlsx'
        wb.save(excel_filename)
        print(f"Exportados {len(rows)} productos exitosamente a {excel_filename}")
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    export_products_excel()
