import sys

def patch_pos_control():
    path = r'src/GestionQ.Web/Views/PosControl/Details.cshtml'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    search = '@if (activeRegister != null)'
    replace = '''@{
        var isCentralPos = Context.Request.Cookies["TerminalPOSId"] == Model.Id.ToString();
    }
    @if (activeRegister != null && isCentralPos)'''

    if search in content and 'isCentralPos' not in content:
        content = content.replace(search, replace, 1)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_pos_control()
