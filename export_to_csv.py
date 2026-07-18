import sqlite3
import csv

def export_products():
    try:
        conn = sqlite3.connect('gestionq.db')
        cursor = conn.cursor()
        
        # Select all from Products table
        cursor.execute('SELECT * FROM Products')
        
        # Get column names
        columns = [description[0] for description in cursor.description]
        
        # Fetch all rows
        rows = cursor.fetchall()
        
        # Write to CSV with BOM for Excel compatibility and semicolon delimiter (standard for Spanish Excel)
        with open('productos.csv', 'w', newline='', encoding='utf-8-sig') as f:
            writer = csv.writer(f, delimiter=';')
            writer.writerow(columns)
            writer.writerows(rows)
            
        print(f"Exportados {len(rows)} productos exitosamente a productos.csv")
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    export_products()
