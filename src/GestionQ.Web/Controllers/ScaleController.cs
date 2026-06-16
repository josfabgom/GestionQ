using GestionQ.Domain.Services;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GestionQ.Web.Controllers;

public class ScaleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IScaleService _scaleService;

    public ScaleController(ApplicationDbContext context, IScaleService scaleService)
    {
        _context = context;
        _scaleService = scaleService;
    }

    public async Task<IActionResult> Index()
    {
        // Obtener productos que son pesables, están explícitamente marcados para enviar, o ya fueron enviados antes
        var pesables = await _context.Products
            .Where(p => (p.IsPesable || p.SendToScale || p.LastSentToScaleDate != null) && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(pesables);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportCatalog()
    {
        var pesables = await _context.Products
            .Where(p => p.SendToScale && p.IsActive)
            .ToListAsync();

        if (!pesables.Any())
        {
            TempData["ErrorMessage"] = "No hay productos marcados para enviar a la balanza.";
            return RedirectToAction(nameof(Index));
        }

        var exportData = pesables.Select(p => (p.InternalCode, p.Name, p.Price)).ToList();
        
        bool success = _scaleService.ExportCatalog(exportData);

        if (success)
        {
            // Marcar como enviados
            foreach (var p in pesables)
            {
                p.SendToScale = false;
                p.LastSentToScaleDate = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Se exportaron {pesables.Count} productos a la carpeta de JDataGate correctamente y se vació la cola de envío.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar exportar el catálogo. Verifique la ruta de JDataGate en Ajustes Generales.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportItegra()
    {
        var pesables = await _context.Products
            .Include(p => p.SubCategory)
            .Where(p => p.SendToScale && p.IsActive)
            .ToListAsync();

        if (!pesables.Any())
        {
            TempData["ErrorMessage"] = "No hay productos marcados para enviar a la balanza.";
            return RedirectToAction(nameof(Index));
        }

        // Para Itegra se requiere: (int Plu, string Name, decimal Price, int CategoryId, bool IsPesable)
        var exportData = pesables.Select(p => (
            Plu: p.InternalCode, 
            Name: p.Name, 
            Price: p.Price, 
            CategoryId: p.SubCategory?.CategoryId ?? 1, // Fallback si no tiene subrubro
            IsPesable: p.IsPesable
        )).ToList();
        
        bool success = _scaleService.ExportItegraCatalog(exportData);

        if (success)
        {
            // Marcar como enviados
            foreach (var p in pesables)
            {
                p.SendToScale = false;
                p.LastSentToScaleDate = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Se exportaron {pesables.Count} productos para Kretz Itegra a la carpeta configurada correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar exportar el catálogo para Kretz Itegra.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkToResend(int[] selectedIds)
    {
        if (selectedIds == null || selectedIds.Length == 0)
        {
            TempData["ErrorMessage"] = "No se seleccionó ningún producto para reenviar.";
            return RedirectToAction(nameof(Index));
        }

        var products = await _context.Products
            .Where(p => selectedIds.Contains(p.Id))
            .ToListAsync();

        foreach (var p in products)
        {
            p.SendToScale = true;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Se marcaron {products.Count} productos como novedad para ser reenviados.";

        return RedirectToAction(nameof(Index));
    }
}
