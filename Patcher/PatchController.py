import sys

def patch_controller():
    path = r'../src/GestionQ.Web/Controllers/CashRegistersController.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Update Index()
    index_search = '''            // Check if current user has an open register
            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);
            ViewBag.HasOpenRegister = openRegister != null;
            if (openRegister != null) ViewBag.OpenRegisterId = openRegister.Id;

            return View(list);'''
            
    index_replace = '''            // Check if current user has an open register
            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);
            ViewBag.HasOpenRegister = openRegister != null;
            if (openRegister != null) ViewBag.OpenRegisterId = openRegister.Id;
            
            bool canClose = false;
            if (openRegister != null)
            {
                var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
                if (!string.IsNullOrEmpty(terminalPosIdStr) && int.TryParse(terminalPosIdStr, out int posId))
                {
                    if (openRegister.PointOfSaleId == posId)
                    {
                        canClose = true;
                    }
                }
            }
            ViewBag.CanCloseRegister = canClose;

            return View(list);'''
            
    if index_search in content:
        content = content.replace(index_search, index_replace)
    else:
        print('Could not find index_search')

    # 2. Update Details()
    details_search = '''if (register == null) return NotFound();

            if (register.IsOpen)'''
            
    details_replace = '''if (register == null) return NotFound();

            bool canCloseDetails = false;
            if (register.IsOpen)
            {
                var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
                if (!string.IsNullOrEmpty(terminalPosIdStr) && int.TryParse(terminalPosIdStr, out int posId))
                {
                    if (register.PointOfSaleId == posId)
                    {
                        canCloseDetails = true;
                    }
                }
            }
            ViewBag.CanCloseRegister = canCloseDetails;

            if (register.IsOpen)'''
            
    if details_search in content:
        content = content.replace(details_search, details_replace)
    else:
        print('Could not find details_search')

    # 3. Update Close GET
    close_get_search = '''[HttpGet]
        public async Task<IActionResult> Close(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();'''
            
    close_get_replace = '''[HttpGet]
        public async Task<IActionResult> Close(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''
            
    if close_get_search in content:
        content = content.replace(close_get_search, close_get_replace)
    else:
        print('Could not find close_get_search')
        
    # 4. Update Close POST
    close_post_search = '''var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return NotFound();

            if (register.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();
            if (register.ClosingDate != null) return RedirectToAction(nameof(Index));'''
            
    close_post_replace = '''var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return NotFound();

            if (register.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();
            if (register.ClosingDate != null) return RedirectToAction(nameof(Index));
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Index));
            }'''

    if close_post_search in content:
        content = content.replace(close_post_search, close_post_replace)
    else:
        print('Could not find close_post_search')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print('Patched CashRegistersController successfully!')

patch_controller()
