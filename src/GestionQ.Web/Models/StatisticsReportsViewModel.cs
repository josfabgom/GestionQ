using System;
using System.Collections.Generic;
using System.Linq;

namespace GestionQ.Web.Models
{
    // ==========================================
    // 1. REPORTE DE RENTABILIDAD / GANANCIAS
    // ==========================================
    public class ProfitabilityViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Totales de Mostrador / Caja (con impuestos)
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCostAmount { get; set; }
        public decimal TotalProfitAmount { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        public decimal ProfitMarkupPercentage { get; set; }

        // Totales Netos Reales (sin impuestos)
        public decimal TotalNetSalesAmount { get; set; }
        public decimal TotalVatAmount { get; set; }
        public decimal TotalInternalTaxAmount { get; set; }
        public decimal TotalRealNetProfit { get; set; }
        public decimal RealNetMarginPercentage { get; set; }
        public decimal RealNetMarkupPercentage { get; set; }

        // Módulo de Compras / Crédito Fiscal
        public decimal TotalPurchasesAmount { get; set; }
        public decimal TotalPurchasesNet { get; set; }
        public decimal TotalPurchaseVatCredit { get; set; }
        public decimal EstimatedVatBalance => TotalVatAmount - TotalPurchaseVatCredit;

        public int ItemsWithoutCostCount => Items.Count(i => i.HasZeroCost);

        public List<ProfitabilityItemViewModel> Items { get; set; } = new();
    }

    public class ProfitabilityItemViewModel
    {
        public int ProductId { get; set; }
        public int InternalCode { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        
        // Costos
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }

        // Venta mostrador (con impuestos)
        public decimal TotalRevenue { get; set; }
        public decimal NetProfit { get; set; }
        public decimal MarginPercentage { get; set; }
        public decimal MarkupPercentage { get; set; }

        // Impuestos
        public decimal VatRatePercentage { get; set; }
        public decimal VatAmount { get; set; }
        public decimal InternalTaxAmount { get; set; }

        // Venta y Ganancia Neta Real (sin impuestos)
        public decimal NetSales { get; set; }
        public decimal RealNetProfit { get; set; }
        public decimal RealMarginPercentage { get; set; }
        public decimal RealMarkupPercentage { get; set; }

        // Pareto y Estado
        public decimal SharePercentage { get; set; }
        public bool HasZeroCost { get; set; }

        public bool HasPresentations => Presentations.Any(p => p.PresentationId.HasValue);
        public List<ProfitabilityPresentationItemViewModel> Presentations { get; set; } = new();
    }

    public class ProfitabilityPresentationItemViewModel
    {
        public int? PresentationId { get; set; }
        public string PresentationName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public bool IsBulk { get; set; }
        public decimal PackageQuantity { get; set; }
        public decimal TotalPackages { get; set; }
        public decimal TotalUnits { get; set; }
        public decimal UnitCost { get; set; }
        public decimal PackageCost { get; set; }
        public decimal TotalCost { get; set; }

        // Venta mostrador
        public decimal TotalRevenue { get; set; }
        public decimal NetProfit { get; set; }
        public decimal MarginPercentage { get; set; }
        public decimal MarkupPercentage { get; set; }

        // Impuestos
        public decimal VatRatePercentage { get; set; }
        public decimal VatAmount { get; set; }
        public decimal InternalTaxAmount { get; set; }

        // Venta y Ganancia Neta Real
        public decimal NetSales { get; set; }
        public decimal RealNetProfit { get; set; }
        public decimal RealMarginPercentage { get; set; }
        public decimal RealMarkupPercentage { get; set; }

        public decimal SharePercentage { get; set; }
        public bool HasZeroCost { get; set; }
    }

    // ==========================================
    // 2. ALERTA DE STOCK CRÍTICO / MÍNIMO
    // ==========================================
    public class LowStockReportViewModel
    {
        public int TotalAlertCount => Items.Count;
        public int OutOfStockCount => Items.Count(i => i.Stock <= 0);
        public int LowStockCount => Items.Count(i => i.Stock > 0);

        public List<LowStockReportItemViewModel> Items { get; set; } = new();
    }

    public class LowStockReportItemViewModel
    {
        public int ProductId { get; set; }
        public int InternalCode { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal Difference => MinimumStock - Stock;
        public decimal UnitCost { get; set; }
        public decimal Price { get; set; }
        public bool IsOutOfStock => Stock <= 0;
    }

    // ==========================================
    // 3. BAJA ROTACIÓN / STOCK ESTANCADO
    // ==========================================
    public class StagnantStockReportViewModel
    {
        public int Days { get; set; } = 30;
        public decimal TotalTiedUpCapital => Items.Sum(i => i.TiedUpCapital);
        public decimal TotalStagnantUnits => Items.Sum(i => i.Stock);
        public int TotalProductsCount => Items.Count;

        public List<StagnantStockReportItemViewModel> Items { get; set; } = new();
    }

    public class StagnantStockReportItemViewModel
    {
        public int ProductId { get; set; }
        public int InternalCode { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TiedUpCapital => Stock * UnitCost;
        public decimal Price { get; set; }
    }
}
