import sys

def patch_pos_controller():
    path = r'src/GestionQ.Web/Controllers/PosControlController.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    search = 'if (register == null) return NotFound();'
    replace = '''if (register == null) return NotFound();
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes forzar el cierre de esta caja. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Details), new { id = register.PointOfSaleId });
            }'''

    if search in content and 'No puedes forzar el cierre' not in content:
        content = content.replace(search, replace, 1)
        print('Patched ForceClose in PosControlController')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_pos_controller()
