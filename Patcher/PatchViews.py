import sys

def patch_views():
    # Index.cshtml
    path = r'../src/GestionQ.Web/Views/CashRegisters/Index.cshtml'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Search for: if (ViewBag.HasOpenRegister)
    if 'if (ViewBag.CanCloseRegister)' not in content:
        content = content.replace('if (ViewBag.HasOpenRegister)', 'if (ViewBag.CanCloseRegister)')
        
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    # Details.cshtml
    path = r'../src/GestionQ.Web/Views/CashRegisters/Details.cshtml'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Search for: if (Model.IsOpen) in the top buttons area.
    # We should only replace the FIRST occurrence in Details.cshtml (which is the button)
    # The other occurrences of Model.IsOpen are for the status badge, etc.
    start_idx = content.find('if (Model.IsOpen)')
    if start_idx != -1 and 'if (ViewBag.CanCloseRegister)' not in content:
        content = content[:start_idx] + 'if (ViewBag.CanCloseRegister)' + content[start_idx + len('if (Model.IsOpen)'):]

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print('Patched Views successfully!')

patch_views()
