using System;
using System.IO;

class Program
{
    static void Main()
    {
        string path = @""src\GestionQ.Web\Controllers\CashRegistersController.cs"";
        string content = File.ReadAllText(path);

        // Update Index()
        string indexSearch = @""// Check if current user has an open register
            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);
            ViewBag.HasOpenRegister = openRegister != null;
            if (openRegister != null) ViewBag.OpenRegisterId = openRegister.Id;

            return View(list);"";
            
        string indexReplace = @""// Check if current user has an open register
            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);
            ViewBag.HasOpenRegister = openRegister != null;
            if (openRegister != null) ViewBag.OpenRegisterId = openRegister.Id;
            
            bool canClose = false;
            if (openRegister != null)
            {
                var terminalPosIdStr = Request.Cookies[""""TerminalPOSId""""];
                if (!string.IsNullOrEmpty(terminalPosIdStr) && int.TryParse(terminalPosIdStr, out int posId))
                {
                    if (openRegister.PointOfSaleId == posId)
                    {
                        canClose = true;
                    }
                }
            }
            ViewBag.CanCloseRegister = canClose;

            return View(list);"";
            
        content = content.Replace(indexSearch, indexReplace);

        // Update Details()
        string detailsSearch = @""if (register == null) return NotFound();

            if (register.IsOpen)"";
            
        string detailsReplace = @""if (register == null) return NotFound();

            bool canCloseDetails = false;
            if (register.IsOpen)
            {
                var terminalPosIdStr = Request.Cookies[""""TerminalPOSId""""];
                if (!string.IsNullOrEmpty(terminalPosIdStr) && int.TryParse(terminalPosIdStr, out int posId))
                {
                    if (register.PointOfSaleId == posId)
                    {
                        canCloseDetails = true;
                    }
                }
            }
            ViewBag.CanCloseRegister = canCloseDetails;

            if (register.IsOpen)"";
            
        content = content.Replace(detailsSearch, detailsReplace);

        // Update Close GET
        string closeGetSearch = @""[HttpGet]
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

            if (register == null) return NotFound();"";
            
        string closeGetReplace = @""[HttpGet]
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
            
            var terminalPosIdStr = Request.Cookies[""""TerminalPOSId""""];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData[""""Message""""] = """"No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió (Ej: Caja POS física)."""";
                return RedirectToAction(nameof(Index));
            }"";
            
        content = content.Replace(closeGetSearch, closeGetReplace);

        // Update Close POST
        string closePostSearch = @""var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return NotFound();

            if (register.UserId != user.Id && !User.IsInRole(""""Admin"""")) return Forbid();
            if (register.ClosingDate != null) return RedirectToAction(nameof(Index));"";
            
        string closePostReplace = @""var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return NotFound();

            if (register.UserId != user.Id && !User.IsInRole(""""Admin"""")) return Forbid();
            if (register.ClosingDate != null) return RedirectToAction(nameof(Index));
            
            var terminalPosIdStr = Request.Cookies[""""TerminalPOSId""""];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData[""""Message""""] = """"No puedes cerrar esta caja desde esta terminal central. Debe cerrarse desde el Punto de Venta donde se abrió."""";
                return RedirectToAction(nameof(Index));
            }"";

        content = content.Replace(closePostSearch, closePostReplace);

        File.WriteAllText(path, content);
        Console.WriteLine(""CashRegistersController updated."");
    }
}
