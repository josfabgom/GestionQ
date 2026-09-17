import sys

def patch_fix():
    path = r'../src/GestionQ.Web/Controllers/CashRegistersController.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    double_insert = '''
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''
            
    single_insert = '''
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''

    content = content.replace(double_insert, single_insert)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

patch_fix()
