import sys

def patch_controller():
    path = r'../src/GestionQ.Web/Controllers/CashRegistersController.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 3. Update Close GET
    # It might be public async Task<IActionResult> Close(int id)
    close_get_start = content.find('public async Task<IActionResult> Close(int id)')
    if close_get_start != -1:
        # Find 'if (register == null) return NotFound();' after this
        not_found_idx = content.find('if (register == null) return NotFound();', close_get_start)
        if not_found_idx != -1:
            insertion_point = not_found_idx + len('if (register == null) return NotFound();')
            # insert the check
            check_code = '''
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''
            content = content[:insertion_point] + check_code + content[insertion_point:]
            print('Patched Close GET')

    # 4. Update Close POST
    close_post_start = content.find('public async Task<IActionResult> Close(int id, string finalCashBalance)')
    if close_post_start != -1:
        not_found_idx = content.find('if (register.ClosingDate != null) return RedirectToAction(nameof(Index));', close_post_start)
        if not_found_idx != -1:
            insertion_point = not_found_idx + len('if (register.ClosingDate != null) return RedirectToAction(nameof(Index));')
            check_code = '''
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''
            content = content[:insertion_point] + check_code + content[insertion_point:]
            print('Patched Close POST')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
patch_controller()
