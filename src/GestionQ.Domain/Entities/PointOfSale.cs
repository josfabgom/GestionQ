using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class PointOfSale
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Punto de Venta")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nro. de Punto de Venta (AFIP/Fiscal)")]
        public int PosNumber { get; set; }

        [Display(Name = "Nombre de la PC / Terminal")]
        public string? MachineName { get; set; }

        [Display(Name = "Descripción/Ubicación")]
        public string? Description { get; set; }

        [Display(Name = "Impresora de Tickets Predeterminada")]
        public string? PrinterName { get; set; }

        [Display(Name = "Número de Copias")]
        public int PrintCopies { get; set; } = 1;

        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Identificador de Caja (Machine Name)")]
        public string? PosIdentifier { get; set; }

        [Display(Name = "Última Sincronización")]
        public DateTime? LastSyncDate { get; set; }

        [Display(Name = "IP de Sincronización")]
        public string? SyncIpAddress { get; set; }

        [Display(Name = "Sincronizar solo con Stock y Precio")]
        public bool SyncOnlyWithStock { get; set; } = true;

        [Display(Name = "Sincronizar Clientes")]
        public bool SyncCustomers { get; set; } = true;

        public List<CashRegister> CashRegisters { get; set; } = new();
        public List<Sale> Sales { get; set; } = new();
    }
}
