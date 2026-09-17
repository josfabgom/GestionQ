import sys

def patch_form1():
    path = r'src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Cambiar el texto del Error Sync
    content = content.replace('⚠\ufe0f Error Sync', '⚠\ufe0f OFFLINE (Local)')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_form1()
