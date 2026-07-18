import re

ocr_text = """
ABUN02 Mix de Bocaditos x 400g - Veg Abundancia ( 21.00 ) 1 5057.55 5057.55
( 21.00 ) 2 1073.85 2147.70 Galletas de Arroz con Sesamos y Sin Sal x 101g -
Arrocitas ARRO02
BIBA03 Leche de Avena x 1l - Biba ( 21.00 ) 1 1771.57 1771.57
( 21.00 ) 1 3419.25 3419.25 Medallones de Calabaza y Garbanzos x 460g -
Burganas BURG03
General -683.85
( 21.00 ) 1 3419.25 3419.25 Medallones de Lentejas y Yamani x 460g -
Burganas BURG06
General -683.85
( 21.00 ) 2 1514.83 3029.66 Galletas de Arroz Dulces Sabor Vainilla x 150g -
Carilo CARI03
CARI05 Galletas de Arroz Integrales x 150g - Carilo ( 21.00 ) 2 1514.83 3029.66
CERR03 Alfajor Dulce de Leche x 55g - Cerro Azul ( 21.00 ) 6 871.06 5226.36
( 21.00 ) 1 6401.42 6401.42 Aceite de Coco Neutro en Aerosol x 190ml - Chia
Graal CHIA17
( 21.00 ) 1 9832.07 9832.07 Chocolatinas 70% Cacao con Stevia x 5g (50u x
caja) - Chocolate Colonial COLO17
( 21.00 ) 1 1968.25 1968.25 Jugo de Arandanos con Stevia x 1.5l - Cuarto
Creciente CUAR01
( 21.00 ) 1 1968.25 1968.25 Jugo de Limonada con Menta y Jengibre con
Stevia x 1.5l - Cuarto Creciente CUAR09
DICO24 Sal Marina Fina 100% Natural x 450g - Dicomere ( 21.00 ) 2 1167.92 2335.84
General Julio -350.38
( 21.00 ) 1 1335.88 1335.88 Sal Marina Ahumada Finas Hierbas x 450g -
Dicomere DICO27
General Julio -200.38
DICO31 Fecula de Maiz x 450g - Dicomere ( 21.00 ) 1 1057.86 1057.86
General Julio -158.68
DICO33 Fecula de Mandioca x 450g - Dicomere ( 21.00 ) 2 1539.88 3079.76
General Julio -461.96

DICO34 Adobo para Pizza x 50g - Dicomere ( 21.00 ) 1 665.84 665.84
General Julio -99.88
DICO35 Aji Molido x 50g - Dicomere ( 21.00 ) 1 719.43 719.43
General Julio -107.91
DICO37 Ajo en Polvo x 50g - Dicomere ( 21.00 ) 1 596.65 596.65
General Julio -89.50
( 21.00 ) 12 1303.38 15640.56 Alfajor Negro con Dulce de Leche Sin Azucar x
60g - Doña Magdalena DOMA10
ENTR07 Aceite Coco Neutro x 200ml - Entrenuts ( 21.00 ) 1 3020.43 3020.43
General Julio -302.04
ENTR08 Aceite Coco Virgen x 200ml - Entrenuts ( 21.00 ) 1 5136.55 5136.55
General Julio -513.66
ENTR09 Aceite Coco Virgen x 360ml - Entrenuts ( 21.00 ) 1 8790.01 8790.01
General Julio -879.00
( 21.00 ) 1 3159.10 3159.10 Pasta de Mani Protein Salted Caramel x 370g -
Entrenuts ENTR25
General Julio -315.91
( 21.00 ) 1 3159.10 3159.10 Pasta de Mani Protein Cookies and Cream x 370g
- Entrenuts ENTR26
General Julio -315.91
ENTR29 Ghee Manteca Carificada x 150g - Entrenuts ( 21.00 ) 1 4284.07 4284.07
General Julio -428.41
( 21.00 ) 5 1203.35 6016.75 Barra Proteica Energetica Frutilla Deli x 45g -
Entrenuts ENTR35
General Julio -601.70
( 21.00 ) 6 934.75 5608.50 Alfajor de Dulce de Leche x 60g - Felices las
Vacas FELI03

FELI15 Karnevil Party (4u) x 320g - Felices las Vacas ( 21.00 ) 1 3954.34 3954.34
General Julio -395.43
FELI16 Nuggets Sabor Pollo x 300g - Felices las Vacas ( 21.00 ) 1 4107.70 4107.70
General Julio -410.77
FELI17 Queso Cremoso x 500g - Felices las Vacas ( 21.00 ) 1 4871.14 4871.14
General Julio -487.11
FELI19 Jogurtti Frutilla x 170g - Felices las Vacas ( 21.00 ) 1 1488.00 1488.00
General Julio -148.80
FELI21 Shogurt de Durazno x 170g - Felices las Vacas ( 21.00 ) 1 1459.94 1459.94
General Julio -145.99
( 21.00 ) 1 3077.51 3077.51 Mila Sabor Pollo (2 uni) x 115g - Felices las
Vacas FELI24
General Julio -307.75
FELI38 Pepas x 300g - Felices las Vacas ( 21.00 ) 1 1916.36 1916.36
General Julio -191.64
( 21.00 ) 2 1715.87 3431.74 Medallon de Espinaca (2uni) x 110g - Felices las
Vacas FELI43
General Julio -343.18
( 21.00 ) 1 1715.87 1715.87 Medallon de Calabaza y Choclo (2uni) x 110g -
Felices las Vacas FELI44
General Julio -171.59
( 21.00 ) 1 1808.73 1808.73 Medallon de Karnevil Party (2uni) x 80g - Felices
las Vacas FELI45
General Julio -180.87
( 21.00 ) 1 2877.77 2877.77 Hummus con Palta y Oliva x 220g - Felices las
Vacas FELI54

( 21.00 ) 1 2070.05 2070.05 Untable Fantastique Finas Hierbas x 200g -
Felices las Vacas FELI58
General Julio -207.01
( 21.00 ) 1 1804.41 1804.41 Queso Cheddar en Fetas x 150g - Felices las
Vacas FELI59
General Julio -180.44
FELI65 Veganesa x 200g - Felices las Vacas ( 21.00 ) 1 1539.85 1539.85
General Julio -153.99
( 21.00 ) 6 1056.35 6338.10 Barras de Frutos Rojos, Arandanos Rojos y Cacao
x 30g - Laddubar GOLD08
General Laddubar -1901.46
( 21.00 ) 6 1056.35 6338.10 Barras de Arandanos, Caju y Arandanos x 30g -
Laddubar GOLD09
General Laddubar -1901.46
( 21.00 ) 6 1267.50 7605.00 Barras de Almendras, Datiles y Cacao x 30g -
Laddubar GOLD20
General Laddubar -1140.78
( 21.00 ) 2 974.46 1948.92 Galletas de Arroz Integral Inflado Dieteticas
Dulces x 100g - Grandiet GRAD06
( 21.00 ) 1 1134.27 1134.27 Galletas de Arroz Integral Inflado Sabor Pizza
con Oregano x 100g - Grandiet GRAD12
INTE13 Barra Caju y Arándanos x 42g - lntegral ( 21.00 ) 5 990.29 4951.45
KARI21 Yogurth Helado Dulce de Leche x 120g - Karinat ( 21.00 ) 1 2665.46 2665.46
General Julio -399.82
KARI22 Yogurth Helado Frutilla x 120g - Karinat ( 21.00 ) 1 2665.46 2665.46
General Julio -399.82

( 21.00 ) 1 2665.46 2665.46 Yogurth Helado Frutos del Bosque x 120g -
Karinat KARI23
General Julio -399.82
KARI25 Yogurth Helado Griego x 120g - Karinat ( 21.00 ) 1 2665.46 2665.46
General Julio -399.82
MACR01 Galletas de Arroz con Sal x 102g - Macrobiotica ( 21.00 ) 2 982.82 1965.64
MACR04 Galletas de Arroz Dulces x 102g - Macrobiotica ( 21.00 ) 2 982.82 1965.64
( 21.00 ) 6 1246.96 7481.76 Not Protein Bar sabor Chocolate Brownie x 45g -
NotCo NOTC148
General Notco -748.20
NTRE14 Milanesa Veggie de Espinaca x 180g - Nutree ( 21.00 ) 1 1826.47 1826.47
( 21.00 ) 1 3374.72 3374.72 Bebida de Aloe Vera King sabor Original x 500ml
- Okf OKFG01
( 21.00 ) 1 3374.72 3374.72 Bebida de Aloe Vera King sabor Blueberry x
500ml - Okf OKFG03
( 21.00 ) 1 3375.27 3375.27 Chimichurri con Vino Malbec x 250g - Pampa
Gourmet PAMP29
General Julio -337.53
PUVI07 Xylitol Azucar Alternativa x 225g - Pure Via ( 21.00 ) 1 13307.62 13307.62
General -1330.76
( 21.00 ) 1 1925.43 1925.43 Yogurt a Base de Coco Arandanos x 170g -
Quimya QUIM03
General -192.54
( 21.00 ) 1 1925.43 1925.43 Yogurt a Base de Coco Mango y Maracuya x 170g
- Quimya QUIM07
General -192.54
QUIM09 Yogurt a Base de Coco Vainilla x 170g - Quimya ( 21.00 ) 1 1925.47 1925.47
General -192.55

QUIM17 Yogurt a Base de Coco Pistacho x 160g - Quimya ( 21.00 ) 1 1925.45 1925.45
General -192.55
( 21.00 ) 1 2172.11 2172.11 Yogurt Colchon sabor Piña Colada Sin Azucar x
150g - Quimya QUIM23
General -217.21
( 21.00 ) 1 7983.78 7983.78 Tarta de Cebolla y Queso x 300g - Santa
Mandioca SANT19
General -798.38
SILK05 Bebida de Almendras Original x 946ml - Silk ( 21.00 ) 2 2739.74 5479.48
General Silk -821.92
( 21.00 ) 2 2739.74 5479.48 Bebida de Almendras Original Sin Azucar x 946ml
- Silk SILK06
General Silk -821.92
SILK08 Bebida de Coco Sin Azucar x 946ml - Silk ( 21.00 ) 2 2739.78 5479.56
General Silk -821.94
SMAM01 Crackers Clasicas x 150g - Smams ( 21.00 ) 2 1900.44 3800.88
General Julio -380.08
SMAM03 Crackers Mix de Semillas x 150g - Smams ( 21.00 ) 2 1900.45 3800.90
General Julio -380.10
SMAM06 Pepas Membrillo x 150g - Smams ( 21.00 ) 1 1578.33 1578.33
General Julio -157.83
SMAM32 Pan Rallado x 350g - Smams ( 21.00 ) 1 2041.17 2041.17
General Julio -204.12
( 21.00 ) 1 6806.54 6806.54 Helado sabor Americana con Frutos Rojos Bajo
en Calorias x 210g - Too Good TOOG01
General -1020.98
TOOG06 Helado sabor Pistacho Keto x 210g - Too Good ( 21.00 ) 1 6806.54 6806.54

TOOG10 Helado sabor Nutella Vegano x 210g - Too Good ( 21.00 ) 1 6806.54 6806.54
General -1020.98
( 21.00 ) 2 2652.69 5305.38 Pasta Seca Multicereal Fusilli con Quinoa x 300g
- Wakas WAKA03
( 21.00 ) 2 2652.69 5305.38 Pasta Seca Multicereal Fusilli con Chia x 300g -
Wakas WAKA07
( 21.00 ) 1 2652.68 2652.68 Pasta Seca Multicereal Penne Rigate de Maiz x
300g - Wakas WAKA13
( 21.00 ) 2 2652.68 5305.36 Pasta Seca Multicereal Fusilli con Amaranto x
300g - Wakas WAKA14
( 21.00 ) 1 2652.68 2652.68 Pasta Multicereal Penne Rigate con Quinoa x
300g - Wakas WAKA15
( 21.00 ) 1 2652.68 2652.68 Pasta Multicereal Penne Rigate con Kale x 300g -
Wakas WAKA16
"""

products = []

import re

# We will collect lines, clean up "General...", etc.
lines = ocr_text.split('\n')
i = 0

def looks_like_product_line(line):
    return re.search(r'\(\s*21\.00\s*\)', line) is not null

buffer = ""
for line in lines:
    line = line.strip()
    if not line or line.startswith('General'):
        continue
    buffer += " " + line
    
    # Try to extract the format: <Code> <Description> ( 21.00 ) <Qty> <Price> <Total>
    # Note that OCR might put the Code at the end or the beginning.
    
    # E.g. "ABUN02 Mix de Bocaditos x 400g - Veg Abundancia ( 21.00 ) 1 5057.55 5057.55"
    # or "( 21.00 ) 2 1073.85 2147.70 Galletas de Arroz con Sesamos y Sin Sal x 101g - Arrocitas ARRO02"
    
    if "( 21.00 )" in buffer:
        # Check if the buffer is complete (we have code, desc, price)
        # Codes are like ABUN02, CARI05 (4 uppercase letters + 2 digits)
        code_match = re.search(r'\b[A-Z]{4}\d{2,3}\b', buffer)
        price_match = re.search(r'\(\s*21\.00\s*\)\s*\d+\s+([\d\.]+)\s+([\d\.]+)', buffer)
        
        if code_match and price_match:
            code = code_match.group(0)
            price = price_match.group(1)
            
            # Clean up the description
            # Remove the code, the ( 21.00 ) block
            desc = buffer.replace(code, '').strip()
            desc = re.sub(r'\(\s*21\.00\s*\)\s*\d+\s+[\d\.]+\s+[\d\.]+', '', desc).strip()
            # Remove leading/trailing dashes or hyphens
            desc = re.sub(r'^-\s+', '', desc)
            desc = re.sub(r'\s+-$', '', desc)
            # Remove excess whitespace
            desc = re.sub(r'\s+', ' ', desc).strip()
            
            products.append({
                'code': code,
                'name': desc,
                'price': price
            })
            buffer = ""

sql = "INSERT INTO Products (SupplierCode, Name, Price, IsActive, CreationDate) VALUES\n"
values = []
for p in products:
    name = p['name'].replace("'", "''")
    values.append(f"('{p['code']}', '{name}', {p['price']}, 1, CURRENT_TIMESTAMP)")

sql += ",\n".join(values) + ";"

with open('d:\\Antigravity Proyectos\\GestionQ\\out\\productos_insert.sql', 'w', encoding='utf-8') as f:
    f.write(sql)
    
print(f"Generated {len(products)} products")
