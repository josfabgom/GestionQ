import sys

def patch_index():
    path = r'../src/GestionQ.Web/Views/CashRegisters/Index.cshtml'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Search for else
    old_else = '''    else
    {
        <a asp-action="Open" class="btn-primary" style="background: #10b981;">💵 Abrir Nueva Caja</a>
    }'''
    
    new_else = '''    else if (!ViewBag.HasOpenRegister)
    {
        <a asp-action="Open" class="btn-primary" style="background: #10b981;">💵 Abrir Nueva Caja</a>
    }'''
    
    if old_else in content:
        content = content.replace(old_else, new_else)
        
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_index()
