using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using System.IO;
using System;

namespace GestionQ.CajaPOS
{
    public class LocalDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<CashRegisterMovement> Movements { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<SalePayment> SalePayments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "caja_local.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Add a shadow property for local syncing of customers
            modelBuilder.Entity<Customer>().Property<bool>("IsSynced").HasDefaultValue(true);

            // Evitar que EF intente crear tablas para dependencias que no usamos localmente
            modelBuilder.Ignore<Microsoft.AspNetCore.Identity.IdentityUser>();
            modelBuilder.Ignore<VatRate>();
            modelBuilder.Ignore<TaxCondition>();
            modelBuilder.Ignore<SubCategory>();
            modelBuilder.Ignore<ProductPrice>();
            modelBuilder.Ignore<CashRegister>();
            modelBuilder.Ignore<PointOfSale>();
            modelBuilder.Ignore<ElectronicInvoice>();
            
            // Ignore navigation properties to avoid Foreign Key constraints
            // which prevent us from easily truncating tables during sync
            modelBuilder.Entity<Sale>().Ignore(s => s.Customer);
            modelBuilder.Entity<SaleItem>().Ignore(si => si.Product);
            modelBuilder.Entity<SalePayment>().Ignore(sp => sp.PaymentMethod);
            modelBuilder.Entity<Department>().Ignore(d => d.VirtualProduct);
        }
    }
}

