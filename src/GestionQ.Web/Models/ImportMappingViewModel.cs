using System.Collections.Generic;

namespace GestionQ.Web.Models
{
    public class ImportMappingViewModel
    {
        public string TempFilePath { get; set; } = string.Empty;
        
        // Mapeos definidos por el usuario (se envían en el POST de vuelta)
        public List<ColumnMapping> Mappings { get; set; } = new List<ColumnMapping>();

        // Filas de ejemplo para previsualizar
        public List<List<string>> SampleRows { get; set; } = new List<List<string>>();
    }
}
