namespace GestionQ.Web.Models
{
    public class ColumnMapping
    {
        public int ExcelColumnIndex { get; set; }
        public string ExcelColumnName { get; set; } = string.Empty;
        public string SystemProperty { get; set; } = string.Empty;
    }
}
