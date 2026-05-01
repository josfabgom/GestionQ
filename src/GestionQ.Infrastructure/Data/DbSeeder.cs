using GestionQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer { Name = "Juan Pérez", Email = "juan.perez@email.com", Phone = "1133445566", Balance = 0 },
                    new Customer { Name = "María Gómez", Email = "maria.gomez@email.com", Phone = "1199887766", Balance = 5000 },
                    new Customer { Name = "Carlos López", Email = "carlos.lopez@email.com", Phone = "1122334455", Balance = -1500 },
                    new Customer { Name = "Ana Martínez", Email = "ana.martinez@email.com", Phone = "1155443322", Balance = 0 },
                    new Customer { Name = "Empresa ABC", Email = "compras@abc.com", Phone = "1166778899", Balance = 15000 }
                };
                context.Customers.AddRange(customers);
            }

            if (!await context.TaxConditions.AnyAsync())
            {
                context.TaxConditions.AddRange(
                    new TaxCondition { Name = "Responsable Inscripto" },
                    new TaxCondition { Name = "Monotributista" },
                    new TaxCondition { Name = "Exento" },
                    new TaxCondition { Name = "Consumidor Final" }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.VatRates.AnyAsync())
            {
                context.VatRates.AddRange(
                    new VatRate { Name = "IVA 21%", Rate = 21 },
                    new VatRate { Name = "IVA 10.5%", Rate = 10.5m },
                    new VatRate { Name = "IVA 0%", Rate = 0 }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Categories.AnyAsync())
            {
                var beverages = new Category { Name = "Bebidas" };
                var grocery = new Category { Name = "Comestibles" };
                var cleaning = new Category { Name = "Limpieza" };

                beverages.SubCategories.Add(new SubCategory { Name = "Gaseosas" });
                beverages.SubCategories.Add(new SubCategory { Name = "Alcohol" });
                grocery.SubCategories.Add(new SubCategory { Name = "Galletitas" });
                grocery.SubCategories.Add(new SubCategory { Name = "Almacén" });
                cleaning.SubCategories.Add(new SubCategory { Name = "Cuidado Personal" });

                context.Categories.AddRange(beverages, grocery, cleaning);
                await context.SaveChangesAsync();
            }

            if (!await context.Products.AnyAsync())
            {
                var subBev = await context.SubCategories.FirstAsync(s => s.Name == "Gaseosas");
                var subGal = await context.SubCategories.FirstAsync(s => s.Name == "Galletitas");
                var subAlm = await context.SubCategories.FirstAsync(s => s.Name == "Almacén");

                var products = new List<Product>
                {
                    new Product { InternalCode = 1, Barcode = "779123456001", Name = "Coca Cola 1.5L", SubCategoryId = subBev.Id, Price = 2500, Stock = 150, IsActive = true },
                    new Product { InternalCode = 2, Barcode = "779123456002", Name = "Sprite 1.5L", SubCategoryId = subBev.Id, Price = 2500, Stock = 80, IsActive = true },
                    new Product { InternalCode = 4, Barcode = "779123456004", Name = "Galletas Oreo 117g", SubCategoryId = subGal.Id, Price = 1200, Stock = 200, IsActive = true },
                    new Product { InternalCode = 6, Barcode = "779123456006", Name = "Yerba Mate Playadito 1Kg", SubCategoryId = subAlm.Id, Price = 4500, Stock = 60, IsActive = true }
                };
                context.Products.AddRange(products);
            }

            if (!await context.PaymentMethods.AnyAsync())
            {
                var paymentMethods = new List<PaymentMethod>
                {
                    new PaymentMethod { Name = "Efectivo", IsActive = true },
                    new PaymentMethod { Name = "Tarjeta de Crédito", IsActive = true },
                    new PaymentMethod { Name = "Tarjeta de Débito", IsActive = true },
                    new PaymentMethod { Name = "Transferencia", IsActive = true },
                    new PaymentMethod { Name = "Cuenta Corriente", IsActive = true }
                };
                context.PaymentMethods.AddRange(paymentMethods);
            }

            await context.SaveChangesAsync();
        }
    }
}
