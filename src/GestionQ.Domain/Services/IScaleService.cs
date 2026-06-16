using System.Collections.Generic;

namespace GestionQ.Domain.Services;

public interface IScaleService
{
    /// <summary>
    /// Exporta un único producto a un archivo para que JDataGate lo procese.
    /// </summary>
    bool ExportProduct(int plu, string name, decimal price);

    /// <summary>
    /// Exporta un catálogo completo de productos para que JDataGate lo procese.
    /// </summary>
    bool ExportCatalog(IEnumerable<(int Plu, string Name, decimal Price)> products);

    /// <summary>
    /// Exporta un catálogo completo de productos para Kretz Itegra en formato CSV.
    /// </summary>
    bool ExportItegraCatalog(IEnumerable<(int Plu, string Name, decimal Price, int CategoryId, bool IsPesable)> products);
}
