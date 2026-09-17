import sys

def patch_controller():
    path = r'../src/GestionQ.Web/Controllers/CashRegistersController.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Update Close POST
    close_post_start = content.find('public async Task<IActionResult> Close(int id, string finalCashBalance)')
    if close_post_start != -1:
        # Instead of searching for the ClosingDate check, let's just insert it after the register lookup.
        # Find 'var register = await _context.CashRegisters.FindAsync(id);'
        lookup_idx = content.find('var register = await _context.CashRegisters.FindAsync(id);', close_post_start)
        if lookup_idx != -1:
            not_found_idx = content.find('if (register == null) return NotFound();', lookup_idx)
            if not_found_idx != -1:
                insertion_point = not_found_idx + len('if (register == null) return NotFound();')
                check_code = '''
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''
                if 'No puedes cerrar esta caja desde esta terminal central.' not in content[insertion_point:insertion_point+300]:
                    content = content[:insertion_point] + check_code + content[insertion_point:]
                    print('Patched Close POST')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
patch_controller()
