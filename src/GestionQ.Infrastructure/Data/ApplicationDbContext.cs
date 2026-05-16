using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using GestionQ.Domain.Entities;

namespace GestionQ.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<SalePayment> SalePayments { get; set; }
        public DbSet<CashRegister> CashRegisters { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<TaxCondition> TaxConditions { get; set; }
        public DbSet<VatRate> VatRates { get; set; }
        public DbSet<ProductPrice> ProductPrices { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<PointOfSale> PointsOfSale { get; set; }
    }
}
