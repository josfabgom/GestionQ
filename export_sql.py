import pyodbc
from openpyxl import Workbook

def export_products_excel():
    drivers = [
        '{ODBC Driver 17 for SQL Server}',
        '{ODBC Driver 18 for SQL Server}',
        '{SQL Server Native Client 11.0}',
        '{SQL Server}'
    ]
    
    conn = None
    for driver in drivers:
        try:
            conn_str = f'Driver={driver};Server=localhost\\SQLEXPRESS;Database=GestionQN;UID=sa;PWD=siste01A;TrustServerCertificate=yes;'
            conn = pyodbc.connect(conn_str)
            print(f"Connected using {driver}")
            break
        except Exception as e:
            continue
            
    if not conn:
        print("Could not connect to SQL Server.")
        return

    try:
        cursor = conn.cursor()
        
        # Select all from Products table (or equivalent table)
        # Let's first check if 'Products' or 'Productos' table exists
        cursor.execute("SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE'")
        tables = [row[0] for row in cursor.fetchall()]
        
        table_name = None
        for t in ['Products', 'Productos', 'Product', 'Articulos']:
            if t in tables:
                table_name = t
                break
                
        if not table_name:
            print(f"Products table not found. Available tables: {tables}")
            return
            
        print(f"Reading from table {table_name}")
        cursor.execute(f'SELECT * FROM [{table_name}]')
        
        # Get column names
        columns = [column[0] for column in cursor.description]
        
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
            # Convert row to list and replace any problematic types if needed
            # pyodbc returns tuples, openpyxl accepts sequences
            ws.append([str(cell) if cell is not None else "" for cell in row])
            
        # Save workbook
        excel_filename = 'productos.xlsx'
        wb.save(excel_filename)
        print(f"Exportados {len(rows)} productos exitosamente a {excel_filename}")
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        if conn:
            conn.close()

if __name__ == "__main__":
    export_products_excel()
