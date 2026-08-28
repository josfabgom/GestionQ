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
        public DbSet<CashRegisterMovement> CashRegisterMovements { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<TaxCondition> TaxConditions { get; set; }
        public DbSet<VatRate> VatRates { get; set; }
        public DbSet<ProductPrice> ProductPrices { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<PointOfSale> PointsOfSale { get; set; }
        public DbSet<ElectronicInvoice> ElectronicInvoices { get; set; }
        public DbSet<FiscalPrintJob> FiscalPrintJobs { get; set; }
        public DbSet<MercadoPagoConfig> MercadoPagoConfigs { get; set; }
        public DbSet<PromotionRule> PromotionRules { get; set; }
        public DbSet<PromotionRuleProduct> PromotionRuleProducts { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<ProductChangeLog> ProductChangeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ElectronicInvoice>()
                .HasOne(e => e.Sale)
                .WithOne(s => s.ElectronicInvoice)
                .HasForeignKey<ElectronicInvoice>(e => e.SaleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FiscalPrintJob>()
                .HasOne(f => f.Sale)
                .WithMany()
                .HasForeignKey(f => f.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PromotionRuleProduct>()
                .HasKey(pr => new { pr.PromotionRuleId, pr.ProductId });

            modelBuilder.Entity<Department>()
                .HasOne(d => d.VirtualProduct)
                .WithMany()
                .HasForeignKey(d => d.VirtualProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override int SaveChanges()
        {
            ProcessProductChanges();
            return base.SaveChanges();
        }

        public override System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            ProcessProductChanges();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ProcessProductChanges()
        {
            var entries = ChangeTracker.Entries<Product>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .ToList(); // Evaluamos antes de agregar nuevos registros

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModified = System.DateTime.Now;
                }

                string changes = "";
                if (entry.State == EntityState.Added)
                {
                    changes = "Creación de artículo";
                }
                else
                {
                    var changeParts = new System.Collections.Generic.List<string>();
                    foreach(var prop in entry.Properties.Where(p => p.IsModified))
                    {
                        var propName = prop.Metadata.Name;
                        if (propName == "LastModified" || propName == "CreationDate") continue;
                        
                        string original = prop.OriginalValue?.ToString() ?? "N/A";
                        string current = prop.CurrentValue?.ToString() ?? "N/A";
                        
                        if (original != current)
                        {
                            string nombreES = propName switch {
                                "Price" => "Precio",
                                "Stock" => "Stock",
                                "Name" => "Nombre",
                                "InternalCode" => "Código Int.",
                                "Barcode" => "Cód. Barras",
                                "IsActive" => "Estado",
                                "Cost" => "Costo",
                                _ => propName
                            };

                            if (propName == "Price" || propName == "Cost") {
                                decimal.TryParse(original, out decimal oPrice);
                                decimal.TryParse(current, out decimal cPrice);
                                original = oPrice.ToString("C2");
                                current = cPrice.ToString("C2");
                            }

                            changeParts.Add($"{nombreES} ({original} ➡️ {current})");
                        }
                    }
                    if (changeParts.Count == 0) continue; 
                    changes = string.Join(", ", changeParts);
                }

                this.Set<ProductChangeLog>().Add(new ProductChangeLog
                {
                    Product = entry.Entity,
                    ProductName = entry.Entity.Name,
                    ChangeDescription = changes,
                    DateChanged = System.DateTime.Now,
                    DateSentToPos = null // Pendiente
                });
            }
        }
    }
}
