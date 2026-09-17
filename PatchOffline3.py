import sys

def patch_form1():
    path = r'src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # The string might be literally "\u26a0\ufe0f Error Sync" in the file if it was decompiled with escape sequences
    content = content.replace(r'btnSync.Text = "⚠\ufe0f Error Sync";', 'btnSync.Text = "\u26A0 OFFLINE (Local)";')
    
    # Let's also do a fallback if it was unescaped
    content = content.replace('Error Sync', 'OFFLINE (Local)')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_form1()
