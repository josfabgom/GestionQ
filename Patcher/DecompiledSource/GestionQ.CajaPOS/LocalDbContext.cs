using System;
using System.IO;
using GestionQ.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.CajaPOS;

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

	public DbSet<SystemSetting> SystemSettings { get; set; }

	public DbSet<OfflineCashRegister> OfflineCashRegisters { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "caja_local.db");
		optionsBuilder.UseSqlite("Data Source=" + text);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<Customer>().Property<bool>("IsSynced").HasDefaultValue(true);
		modelBuilder.Ignore<IdentityUser>();
		modelBuilder.Ignore<VatRate>();
		modelBuilder.Ignore<TaxCondition>();
		modelBuilder.Ignore<SubCategory>();
		modelBuilder.Ignore<ProductPrice>();
		modelBuilder.Ignore<CashRegister>();
		modelBuilder.Ignore<PointOfSale>();
		modelBuilder.Ignore<ElectronicInvoice>();
		modelBuilder.Entity<Sale>().Ignore((Sale s) => s.Customer);
		modelBuilder.Entity<SaleItem>().Ignore((SaleItem si) => si.Product);
		modelBuilder.Entity<SalePayment>().Ignore((SalePayment sp) => sp.PaymentMethod);
		modelBuilder.Entity<Department>().Ignore((Department d) => d.VirtualProduct);
	}
}
