using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GestionQ.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionQ.Infrastructure.Services;

public class KretzJDataGateService : IScaleService
{
    private readonly ILogger<KretzJDataGateService> _logger;
    private readonly string _jDataGateFolder;

    public KretzJDataGateService(ILogger<KretzJDataGateService> logger, IConfiguration configuration)
    {
        _logger = logger;
        // Se puede configurar en appsettings.json bajo "Scale:JDataGateFolderPath"
        _jDataGateFolder = configuration["Scale:JDataGateFolderPath"] ?? @"C:\JDataGate\IN\";
        
        if (!Directory.Exists(_jDataGateFolder))
        {
            try
            {
                Directory.CreateDirectory(_jDataGateFolder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo crear el directorio de JDataGate {Folder}", _jDataGateFolder);
            }
        }
    }

    public bool ExportProduct(int plu, string name, decimal price)
    {
        return ExportCatalog(new[] { (plu, name, price) });
    }

    public bool ExportCatalog(IEnumerable<(int Plu, string Name, decimal Price)> products)
    {
        try
        {
            if (!Directory.Exists(_jDataGateFolder))
            {
                Directory.CreateDirectory(_jDataGateFolder);
            }

            // Generamos un archivo único por exportación
            string fileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(_jDataGateFolder, fileName);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            foreach (var product in products)
            {
                // Formato estándar delimitado por tubería (|)
                // PLU | Descripción | Precio
                // JDataGate permite configurar el separador y mapear las columnas.
                string formattedName = product.Name.Length > 25 ? product.Name.Substring(0, 25) : product.Name;
                string formattedPrice = product.Price.ToString("0.00").Replace(",", ".");
                
                writer.WriteLine($"{product.Plu}|{formattedName}|{formattedPrice}");
            }

            _logger.LogInformation("Catálogo exportado exitosamente a {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar catálogo para JDataGate.");
            return false;
        }
    }

    public bool ExportItegraCatalog(IEnumerable<(int Plu, string Name, decimal Price, int CategoryId, bool IsPesable)> products)
    {
        try
        {
            if (!Directory.Exists(_jDataGateFolder))
            {
                Directory.CreateDirectory(_jDataGateFolder);
            }

            // Generamos un archivo único por exportación para Itegra
            string fileName = $"itegra_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(_jDataGateFolder, fileName);

            // Usamos UTF8 sin BOM (Byte Order Mark) para evitar el caracter extraño al inicio del archivo
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(false));

            foreach (var product in products)
            {
                // NUMERO DE PLU: 6 dígitos
                string pluNum = product.Plu.ToString().PadLeft(6, '0');
                
                // CODIGO DE PLU: 13 dígitos
                string pluCod = product.Plu.ToString().PadLeft(13, '0');
                
                // NOMBRE DE PLU: 26 caracteres
                string name = product.Name.Length > 26 ? product.Name.Substring(0, 26) : product.Name;
                name = name.PadRight(26, ' ');
                
                // CODIGO DE DEPARTAMENTO: sin padding o el que corresponda (en el ejemplo es 1 dígito)
                string dpto = product.CategoryId.ToString();
                
                // PRECIO: Formato con punto decimal, asumiendo que Itegra lo lee correctamente si está delimitado
                string price = Math.Round(product.Price, 2).ToString("0.00").Replace(",", ".");
                
                // TIPO DE PLU: 1 carácter – N no pesable – P pesable
                string type = product.IsPesable ? "P" : "N";
                
                // CODIGO DE ETIQUETA: 2 dígitos
                string labelCode = "01"; // Fijo según requerimiento

                // Formato con delimitador punto y coma (;)
                writer.WriteLine($"{pluNum};{pluCod};{name};{price};{dpto};{type};{labelCode}");
            }

            _logger.LogInformation("Catálogo para Itegra exportado exitosamente a {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar catálogo para Itegra.");
            return false;
        }
    }
}
