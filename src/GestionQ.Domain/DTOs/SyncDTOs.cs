using System;
using System.Collections.Generic;

namespace GestionQ.Domain.DTOs
{
    public class SyncPullRequest
    {
        public DateTime? LastSyncDate { get; set; }
        public string PosIdentifier { get; set; } = string.Empty;
    }

    public class SyncPullResponse
    {
        public List<ProductSyncDto> Products { get; set; } = new();
        public List<CustomerSyncDto> Customers { get; set; } = new();
        public List<DepartmentSyncDto> Departments { get; set; } = new();
        public List<PaymentMethodSyncDto> PaymentMethods { get; set; } = new();
    }

    public class PaymentMethodSyncDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime? DiscountValidFrom { get; set; }
        public DateTime? DiscountValidTo { get; set; }
    }

    public class CustomerSyncDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Dni { get; set; }
        public string? Cuit { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentSyncDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Hotkey { get; set; }
        public int VirtualProductId { get; set; }
    }

    public class ProductSyncDto
    {
        public int Id { get; set; }
        public int InternalCode { get; set; }
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class SyncPushRequest
    {
        public string PosIdentifier { get; set; } = string.Empty;
        public List<CustomerSyncDto> NewCustomers { get; set; } = new();
        public List<SaleSyncDto> Sales { get; set; } = new();
        public List<MovementSyncDto> Movements { get; set; } = new();
    }

    public class SaleSyncDto
    {
        public Guid GlobalId { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaymentDiscountAmount { get; set; }
        public string? UserId { get; set; }
        public int? CashRegisterId { get; set; }
        public string? CustomerDni { get; set; } // Used to match customer
        public bool RequestElectronicInvoice { get; set; }
        public List<SaleItemSyncDto> Items { get; set; } = new();
        public List<SalePaymentSyncDto> Payments { get; set; } = new();
    }

    public class SalePaymentSyncDto
    {
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionReference { get; set; }
    }

    public class SaleItemSyncDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class MovementSyncDto
    {
        public Guid GlobalId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int? CashRegisterId { get; set; }
    }

    // POS Auth & Register DTOs

    public class PosLoginRequestDto
    {
        public string Pin { get; set; } = string.Empty;
        public string PosIdentifier { get; set; } = string.Empty;
    }

    public class PosLoginResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
    }

    public class PosOpenRegisterRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string PosIdentifier { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
    }

    public class PosOpenRegisterResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? CashRegisterId { get; set; }
    }

    public class PosCloseRegisterRequestDto
    {
        public int CashRegisterId { get; set; }
        public decimal FinalCashBalance { get; set; }
    }

    public class PosStatusResponseDto
    {
        public bool HasOpenRegister { get; set; }
        public int? CashRegisterId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
    }
}

