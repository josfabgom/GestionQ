import sys

def patch_auth_client():
    path = r'src/GestionQ.CajaPOS/AuthClient.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Reducir el timeout de Login para que entre más rápido si no hay internet
    search = 'TimeSpan.FromSeconds(5);'
    replace = 'TimeSpan.FromSeconds(1.5);'
    if search in content:
        content = content.replace(search, replace, 1)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

def patch_form1():
    path = r'src/GestionQ.CajaPOS/Form1.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Cambiar el texto del OnSyncError
    search1 = 'btnSync.Text = "⚠\ufe0f Error Sync";'
    replace1 = 'btnSync.Text = "⚠\ufe0f MODO OFFLINE (Local)";'
    if search1 in content:
        content = content.replace(search1, replace1)

    # 2. Eliminar el MessageBox bloqueante al hacer click manual en Sincronizar
    search2 = 'MessageBox.Show("No se pudo conectar con la central en " + AppConfig.ServerUrl + ".\\nError técnico: " + ex2.Message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);'
    replace2 = '/* MessageBox.Show bloqueante removido para UX Offline */'
    if search2 in content:
        content = content.replace(search2, replace2)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_auth_client()
patch_form1()
