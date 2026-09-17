using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace GestionQ.CajaPOS.Migrations;

[DbContext(typeof(LocalDbContext))]
[Migration("20260820014532_InitialLocal")]
public class InitialLocal : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable("Category", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id7 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			int? maxLength7 = 100;
			return new
			{
				Id = id7,
				Name = table.Column<string>("TEXT", null, maxLength7)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Category", x => x.Id);
		});
		migrationBuilder.CreateTable("PaymentMethod", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id6 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			int? maxLength6 = 100;
			return new
			{
				Id = id6,
				Name = table.Column<string>("TEXT", null, maxLength6),
				IsActive = table.Column<bool>("INTEGER"),
				DiscountPercentage = table.Column<decimal>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_PaymentMethod", x => x.Id);
		});
		migrationBuilder.CreateTable("PointOfSale", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			Name = table.Column<string>("TEXT"),
			PosNumber = table.Column<int>("INTEGER"),
			MachineName = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			Description = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			PrinterName = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			PrintCopies = table.Column<int>("INTEGER"),
			IsActive = table.Column<bool>("INTEGER")
		}, null, table =>
		{
			table.PrimaryKey("PK_PointOfSale", x => x.Id);
		});
		migrationBuilder.CreateTable("TaxCondition", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			Name = table.Column<string>("TEXT"),
			IsActive = table.Column<bool>("INTEGER")
		}, null, table =>
		{
			table.PrimaryKey("PK_TaxCondition", x => x.Id);
		});
		migrationBuilder.CreateTable("VatRate", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			Name = table.Column<string>("TEXT"),
			Rate = table.Column<decimal>("TEXT"),
			IsActive = table.Column<bool>("INTEGER")
		}, null, table =>
		{
			table.PrimaryKey("PK_VatRate", x => x.Id);
		});
		migrationBuilder.CreateTable("SubCategory", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id5 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			int? maxLength5 = 100;
			return new
			{
				Id = id5,
				Name = table.Column<string>("TEXT", null, maxLength5),
				CategoryId = table.Column<int>("INTEGER")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_SubCategory", x => x.Id);
			table.ForeignKey("FK_SubCategory_Category_CategoryId", x => x.CategoryId, "Category", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateTable("CashRegister", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			UserId = table.Column<string>("TEXT"),
			OpeningDate = table.Column<DateTime>("TEXT"),
			ClosingDate = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true),
			InitialBalance = table.Column<decimal>("TEXT"),
			ExpectedCashBalance = table.Column<decimal>("TEXT", null, null, rowVersion: false, null, nullable: true),
			FinalCashBalance = table.Column<decimal>("TEXT", null, null, rowVersion: false, null, nullable: true),
			Difference = table.Column<decimal>("TEXT", null, null, rowVersion: false, null, nullable: true),
			PointOfSaleId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true)
		}, null, table =>
		{
			table.PrimaryKey("PK_CashRegister", x => x.Id);
			table.ForeignKey("FK_CashRegister_PointOfSale_PointOfSaleId", x => x.PointOfSaleId, "PointOfSale", "Id");
		});
		migrationBuilder.CreateTable("Customer", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id4 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			int? maxLength4 = 100;
			return new
			{
				Id = id4,
				Name = table.Column<string>("TEXT", null, maxLength4),
				Email = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Phone = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Dni = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Cuit = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				TaxConditionId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
				Address = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Locality = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				IsActive = table.Column<bool>("INTEGER"),
				InternalCode = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				ImageUrl = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Balance = table.Column<decimal>("TEXT"),
				DiscountPercentage = table.Column<decimal>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Customer", x => x.Id);
			table.ForeignKey("FK_Customer_TaxCondition_TaxConditionId", x => x.TaxConditionId, "TaxCondition", "Id");
		});
		migrationBuilder.CreateTable("Products", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id3 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			OperationBuilder<AddColumnOperation> internalCode = table.Column<int>("INTEGER");
			int? maxLength3 = 50;
			OperationBuilder<AddColumnOperation> barcode = table.Column<string>("TEXT", null, maxLength3, rowVersion: false, null, nullable: true);
			maxLength3 = 50;
			OperationBuilder<AddColumnOperation> supplierCode = table.Column<string>("TEXT", null, maxLength3, rowVersion: false, null, nullable: true);
			maxLength3 = 100;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("TEXT", null, maxLength3);
			maxLength3 = 30;
			return new
			{
				Id = id3,
				InternalCode = internalCode,
				Barcode = barcode,
				SupplierCode = supplierCode,
				Name = name,
				ShortDescriptionScale = table.Column<string>("TEXT", null, maxLength3, rowVersion: false, null, nullable: true),
				SubCategoryId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
				IsPesable = table.Column<bool>("INTEGER"),
				IsFractionable = table.Column<bool>("INTEGER"),
				SendToScale = table.Column<bool>("INTEGER"),
				IsScaleNovelty = table.Column<bool>("INTEGER"),
				LastSentToScaleDate = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true),
				Price = table.Column<decimal>("TEXT"),
				Stock = table.Column<decimal>("TEXT"),
				MinimumStock = table.Column<decimal>("TEXT"),
				VatRateId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
				ImageUrl = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
				CreationDate = table.Column<DateTime>("TEXT"),
				LastModified = table.Column<DateTime>("TEXT"),
				ExpirationDays = table.Column<int>("INTEGER"),
				IsDepartment = table.Column<bool>("INTEGER"),
				IsActive = table.Column<bool>("INTEGER"),
				NeedsLabelPrint = table.Column<bool>("INTEGER")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Products", x => x.Id);
			table.ForeignKey("FK_Products_SubCategory_SubCategoryId", x => x.SubCategoryId, "SubCategory", "Id");
			table.ForeignKey("FK_Products_VatRate_VatRateId", x => x.VatRateId, "VatRate", "Id");
		});
		migrationBuilder.CreateTable("Movements", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id2 = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			OperationBuilder<AddColumnOperation> globalId = table.Column<Guid>("TEXT");
			OperationBuilder<AddColumnOperation> isSynced = table.Column<bool>("INTEGER");
			OperationBuilder<AddColumnOperation> syncedAt = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true);
			OperationBuilder<AddColumnOperation> cashRegisterId = table.Column<int>("INTEGER");
			OperationBuilder<AddColumnOperation> amount = table.Column<decimal>("TEXT");
			int? maxLength2 = 20;
			OperationBuilder<AddColumnOperation> type = table.Column<string>("TEXT", null, maxLength2);
			maxLength2 = 200;
			return new
			{
				Id = id2,
				GlobalId = globalId,
				IsSynced = isSynced,
				SyncedAt = syncedAt,
				CashRegisterId = cashRegisterId,
				Amount = amount,
				Type = type,
				Description = table.Column<string>("TEXT", null, maxLength2),
				Date = table.Column<DateTime>("TEXT")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Movements", x => x.Id);
			table.ForeignKey("FK_Movements_CashRegister_CashRegisterId", x => x.CashRegisterId, "CashRegister", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateTable("Sales", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			GlobalId = table.Column<Guid>("TEXT"),
			IsSynced = table.Column<bool>("INTEGER"),
			SyncedAt = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true),
			Date = table.Column<DateTime>("TEXT"),
			CustomerId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
			TotalAmount = table.Column<decimal>("TEXT"),
			SubTotal = table.Column<decimal>("TEXT"),
			DiscountAmount = table.Column<decimal>("TEXT"),
			PaymentDiscountAmount = table.Column<decimal>("TEXT"),
			UserId = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			CashRegisterId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
			PointOfSaleId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
			IsCancelled = table.Column<bool>("INTEGER"),
			CancellationDate = table.Column<DateTime>("TEXT", null, null, rowVersion: false, null, nullable: true)
		}, null, table =>
		{
			table.PrimaryKey("PK_Sales", x => x.Id);
			table.ForeignKey("FK_Sales_CashRegister_CashRegisterId", x => x.CashRegisterId, "CashRegister", "Id");
			table.ForeignKey("FK_Sales_Customer_CustomerId", x => x.CustomerId, "Customer", "Id");
			table.ForeignKey("FK_Sales_PointOfSale_PointOfSaleId", x => x.PointOfSaleId, "PointOfSale", "Id");
		});
		migrationBuilder.CreateTable("ProductPrice", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			ProductId = table.Column<int>("INTEGER"),
			BaseCost = table.Column<decimal>("TEXT"),
			ProfitMargin = table.Column<decimal>("TEXT"),
			InternalTax = table.Column<decimal>("TEXT"),
			FinalPrice = table.Column<decimal>("TEXT"),
			UpdateDate = table.Column<DateTime>("TEXT")
		}, null, table =>
		{
			table.PrimaryKey("PK_ProductPrice", x => x.Id);
			table.ForeignKey("FK_ProductPrice_Products_ProductId", x => x.ProductId, "Products", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateTable("ElectronicInvoice", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			SaleId = table.Column<int>("INTEGER", null, null, rowVersion: false, null, nullable: true),
			PointOfSaleId = table.Column<int>("INTEGER"),
			PointOfSaleNumber = table.Column<int>("INTEGER"),
			InvoiceTypeCode = table.Column<int>("INTEGER"),
			InvoiceTypeDesc = table.Column<string>("TEXT"),
			InvoiceNumber = table.Column<int>("INTEGER"),
			IssueDate = table.Column<DateTime>("TEXT"),
			ConceptCode = table.Column<int>("INTEGER"),
			DocTypeCode = table.Column<int>("INTEGER"),
			DocNumber = table.Column<string>("TEXT"),
			CustomerName = table.Column<string>("TEXT"),
			CustomerTaxCondition = table.Column<string>("TEXT"),
			NetAmount = table.Column<decimal>("TEXT"),
			VatAmount = table.Column<decimal>("TEXT"),
			ExemptAmount = table.Column<decimal>("TEXT"),
			TotalAmount = table.Column<decimal>("TEXT"),
			CAE = table.Column<string>("TEXT"),
			CAEExpirationDate = table.Column<DateTime>("TEXT"),
			Status = table.Column<string>("TEXT"),
			ErrorMessage = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true),
			CanMisMonExt = table.Column<bool>("INTEGER"),
			CondicionIVAReceptorId = table.Column<int>("INTEGER")
		}, null, table =>
		{
			table.PrimaryKey("PK_ElectronicInvoice", x => x.Id);
			table.ForeignKey("FK_ElectronicInvoice_PointOfSale_PointOfSaleId", x => x.PointOfSaleId, "PointOfSale", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
			table.ForeignKey("FK_ElectronicInvoice_Sales_SaleId", x => x.SaleId, "Sales", "Id");
		});
		migrationBuilder.CreateTable("SaleItems", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true);
			OperationBuilder<AddColumnOperation> saleId = table.Column<int>("INTEGER");
			OperationBuilder<AddColumnOperation> productId = table.Column<int>("INTEGER");
			OperationBuilder<AddColumnOperation> quantity = table.Column<decimal>("TEXT");
			OperationBuilder<AddColumnOperation> unitPrice = table.Column<decimal>("TEXT");
			OperationBuilder<AddColumnOperation> discountAmount = table.Column<decimal>("TEXT");
			int? maxLength = 150;
			return new
			{
				Id = id,
				SaleId = saleId,
				ProductId = productId,
				Quantity = quantity,
				UnitPrice = unitPrice,
				DiscountAmount = discountAmount,
				CustomName = table.Column<string>("TEXT", null, maxLength, rowVersion: false, null, nullable: true)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_SaleItems", x => x.Id);
			table.ForeignKey("FK_SaleItems_Products_ProductId", x => x.ProductId, "Products", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
			table.ForeignKey("FK_SaleItems_Sales_SaleId", x => x.SaleId, "Sales", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateTable("SalePayment", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("INTEGER").Annotation("Sqlite:Autoincrement", true),
			SaleId = table.Column<int>("INTEGER"),
			PaymentMethodId = table.Column<int>("INTEGER"),
			Amount = table.Column<decimal>("TEXT"),
			TransactionReference = table.Column<string>("TEXT", null, null, rowVersion: false, null, nullable: true)
		}, null, table =>
		{
			table.PrimaryKey("PK_SalePayment", x => x.Id);
			table.ForeignKey("FK_SalePayment_PaymentMethod_PaymentMethodId", x => x.PaymentMethodId, "PaymentMethod", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
			table.ForeignKey("FK_SalePayment_Sales_SaleId", x => x.SaleId, "Sales", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateIndex("IX_CashRegister_PointOfSaleId", "CashRegister", "PointOfSaleId");
		migrationBuilder.CreateIndex("IX_Customer_TaxConditionId", "Customer", "TaxConditionId");
		migrationBuilder.CreateIndex("IX_ElectronicInvoice_PointOfSaleId", "ElectronicInvoice", "PointOfSaleId");
		migrationBuilder.CreateIndex("IX_ElectronicInvoice_SaleId", "ElectronicInvoice", "SaleId", null, unique: true);
		migrationBuilder.CreateIndex("IX_Movements_CashRegisterId", "Movements", "CashRegisterId");
		migrationBuilder.CreateIndex("IX_ProductPrice_ProductId", "ProductPrice", "ProductId");
		migrationBuilder.CreateIndex("IX_Products_SubCategoryId", "Products", "SubCategoryId");
		migrationBuilder.CreateIndex("IX_Products_VatRateId", "Products", "VatRateId");
		migrationBuilder.CreateIndex("IX_SaleItems_ProductId", "SaleItems", "ProductId");
		migrationBuilder.CreateIndex("IX_SaleItems_SaleId", "SaleItems", "SaleId");
		migrationBuilder.CreateIndex("IX_SalePayment_PaymentMethodId", "SalePayment", "PaymentMethodId");
		migrationBuilder.CreateIndex("IX_SalePayment_SaleId", "SalePayment", "SaleId");
		migrationBuilder.CreateIndex("IX_Sales_CashRegisterId", "Sales", "CashRegisterId");
		migrationBuilder.CreateIndex("IX_Sales_CustomerId", "Sales", "CustomerId");
		migrationBuilder.CreateIndex("IX_Sales_PointOfSaleId", "Sales", "PointOfSaleId");
		migrationBuilder.CreateIndex("IX_SubCategory_CategoryId", "SubCategory", "CategoryId");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("ElectronicInvoice");
		migrationBuilder.DropTable("Movements");
		migrationBuilder.DropTable("ProductPrice");
		migrationBuilder.DropTable("SaleItems");
		migrationBuilder.DropTable("SalePayment");
		migrationBuilder.DropTable("Products");
		migrationBuilder.DropTable("PaymentMethod");
		migrationBuilder.DropTable("Sales");
		migrationBuilder.DropTable("SubCategory");
		migrationBuilder.DropTable("VatRate");
		migrationBuilder.DropTable("CashRegister");
		migrationBuilder.DropTable("Customer");
		migrationBuilder.DropTable("Category");
		migrationBuilder.DropTable("PointOfSale");
		migrationBuilder.DropTable("TaxCondition");
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", "9.0.15");
		modelBuilder.Entity("GestionQ.Domain.Entities.CashRegister", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<DateTime?>("ClosingDate").HasColumnType("TEXT");
			b.Property<decimal?>("Difference").HasColumnType("TEXT");
			b.Property<decimal?>("ExpectedCashBalance").HasColumnType("TEXT");
			b.Property<decimal?>("FinalCashBalance").HasColumnType("TEXT");
			b.Property<decimal>("InitialBalance").HasColumnType("TEXT");
			b.Property<DateTime>("OpeningDate").HasColumnType("TEXT");
			b.Property<int?>("PointOfSaleId").HasColumnType("INTEGER");
			b.Property<string>("UserId").IsRequired().HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("PointOfSaleId");
			b.ToTable("CashRegister");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.CashRegisterMovement", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<decimal>("Amount").HasColumnType("TEXT");
			b.Property<int>("CashRegisterId").HasColumnType("INTEGER");
			b.Property<DateTime>("Date").HasColumnType("TEXT");
			b.Property<string>("Description").IsRequired().HasMaxLength(200)
				.HasColumnType("TEXT");
			b.Property<Guid>("GlobalId").HasColumnType("TEXT");
			b.Property<bool>("IsSynced").HasColumnType("INTEGER");
			b.Property<DateTime?>("SyncedAt").HasColumnType("TEXT");
			b.Property<string>("Type").IsRequired().HasMaxLength(20)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("CashRegisterId");
			b.ToTable("Movements");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Category", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasMaxLength(100)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("Category");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Customer", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("Address").HasColumnType("TEXT");
			b.Property<decimal>("Balance").HasColumnType("TEXT");
			b.Property<string>("Cuit").HasColumnType("TEXT");
			b.Property<decimal>("DiscountPercentage").HasColumnType("TEXT");
			b.Property<string>("Dni").HasColumnType("TEXT");
			b.Property<string>("Email").HasColumnType("TEXT");
			b.Property<string>("ImageUrl").HasColumnType("TEXT");
			b.Property<string>("InternalCode").HasColumnType("TEXT");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<string>("Locality").HasColumnType("TEXT");
			b.Property<string>("Name").IsRequired().HasMaxLength(100)
				.HasColumnType("TEXT");
			b.Property<string>("Phone").HasColumnType("TEXT");
			b.Property<int?>("TaxConditionId").HasColumnType("INTEGER");
			b.HasKey("Id");
			b.HasIndex("TaxConditionId");
			b.ToTable("Customer");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.ElectronicInvoice", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("CAE").IsRequired().HasColumnType("TEXT");
			b.Property<DateTime>("CAEExpirationDate").HasColumnType("TEXT");
			b.Property<bool>("CanMisMonExt").HasColumnType("INTEGER");
			b.Property<int>("ConceptCode").HasColumnType("INTEGER");
			b.Property<int>("CondicionIVAReceptorId").HasColumnType("INTEGER");
			b.Property<string>("CustomerName").IsRequired().HasColumnType("TEXT");
			b.Property<string>("CustomerTaxCondition").IsRequired().HasColumnType("TEXT");
			b.Property<string>("DocNumber").IsRequired().HasColumnType("TEXT");
			b.Property<int>("DocTypeCode").HasColumnType("INTEGER");
			b.Property<string>("ErrorMessage").HasColumnType("TEXT");
			b.Property<decimal>("ExemptAmount").HasColumnType("TEXT");
			b.Property<int>("InvoiceNumber").HasColumnType("INTEGER");
			b.Property<int>("InvoiceTypeCode").HasColumnType("INTEGER");
			b.Property<string>("InvoiceTypeDesc").IsRequired().HasColumnType("TEXT");
			b.Property<DateTime>("IssueDate").HasColumnType("TEXT");
			b.Property<decimal>("NetAmount").HasColumnType("TEXT");
			b.Property<int>("PointOfSaleId").HasColumnType("INTEGER");
			b.Property<int>("PointOfSaleNumber").HasColumnType("INTEGER");
			b.Property<int?>("SaleId").HasColumnType("INTEGER");
			b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
			b.Property<decimal>("TotalAmount").HasColumnType("TEXT");
			b.Property<decimal>("VatAmount").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("PointOfSaleId");
			b.HasIndex("SaleId").IsUnique();
			b.ToTable("ElectronicInvoice");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.PaymentMethod", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<decimal>("DiscountPercentage").HasColumnType("TEXT");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasMaxLength(100)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("PaymentMethod");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.PointOfSale", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("Description").HasColumnType("TEXT");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<string>("MachineName").HasColumnType("TEXT");
			b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
			b.Property<int>("PosNumber").HasColumnType("INTEGER");
			b.Property<int>("PrintCopies").HasColumnType("INTEGER");
			b.Property<string>("PrinterName").HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("PointOfSale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Product", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("Barcode").HasMaxLength(50).HasColumnType("TEXT");
			b.Property<DateTime>("CreationDate").HasColumnType("TEXT");
			b.Property<int>("ExpirationDays").HasColumnType("INTEGER");
			b.Property<string>("ImageUrl").HasColumnType("TEXT");
			b.Property<int>("InternalCode").HasColumnType("INTEGER");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<bool>("IsDepartment").HasColumnType("INTEGER");
			b.Property<bool>("IsFractionable").HasColumnType("INTEGER");
			b.Property<bool>("IsPesable").HasColumnType("INTEGER");
			b.Property<bool>("IsScaleNovelty").HasColumnType("INTEGER");
			b.Property<DateTime>("LastModified").HasColumnType("TEXT");
			b.Property<DateTime?>("LastSentToScaleDate").HasColumnType("TEXT");
			b.Property<decimal>("MinimumStock").HasColumnType("TEXT");
			b.Property<string>("Name").IsRequired().HasMaxLength(100)
				.HasColumnType("TEXT");
			b.Property<bool>("NeedsLabelPrint").HasColumnType("INTEGER");
			b.Property<decimal>("Price").HasColumnType("TEXT");
			b.Property<bool>("SendToScale").HasColumnType("INTEGER");
			b.Property<string>("ShortDescriptionScale").HasMaxLength(30).HasColumnType("TEXT");
			b.Property<decimal>("Stock").HasColumnType("TEXT");
			b.Property<int?>("SubCategoryId").HasColumnType("INTEGER");
			b.Property<string>("SupplierCode").HasMaxLength(50).HasColumnType("TEXT");
			b.Property<int?>("VatRateId").HasColumnType("INTEGER");
			b.HasKey("Id");
			b.HasIndex("SubCategoryId");
			b.HasIndex("VatRateId");
			b.ToTable("Products");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.ProductPrice", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<decimal>("BaseCost").HasColumnType("TEXT");
			b.Property<decimal>("FinalPrice").HasColumnType("TEXT");
			b.Property<decimal>("InternalTax").HasColumnType("TEXT");
			b.Property<int>("ProductId").HasColumnType("INTEGER");
			b.Property<decimal>("ProfitMargin").HasColumnType("TEXT");
			b.Property<DateTime>("UpdateDate").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("ProductId");
			b.ToTable("ProductPrice");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Sale", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<DateTime?>("CancellationDate").HasColumnType("TEXT");
			b.Property<int?>("CashRegisterId").HasColumnType("INTEGER");
			b.Property<int?>("CustomerId").HasColumnType("INTEGER");
			b.Property<DateTime>("Date").HasColumnType("TEXT");
			b.Property<decimal>("DiscountAmount").HasColumnType("TEXT");
			b.Property<Guid>("GlobalId").HasColumnType("TEXT");
			b.Property<bool>("IsCancelled").HasColumnType("INTEGER");
			b.Property<bool>("IsSynced").HasColumnType("INTEGER");
			b.Property<decimal>("PaymentDiscountAmount").HasColumnType("TEXT");
			b.Property<int?>("PointOfSaleId").HasColumnType("INTEGER");
			b.Property<decimal>("SubTotal").HasColumnType("TEXT");
			b.Property<DateTime?>("SyncedAt").HasColumnType("TEXT");
			b.Property<decimal>("TotalAmount").HasColumnType("TEXT");
			b.Property<string>("UserId").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("CashRegisterId");
			b.HasIndex("CustomerId");
			b.HasIndex("PointOfSaleId");
			b.ToTable("Sales");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SaleItem", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<string>("CustomName").HasMaxLength(150).HasColumnType("TEXT");
			b.Property<decimal>("DiscountAmount").HasColumnType("TEXT");
			b.Property<int>("ProductId").HasColumnType("INTEGER");
			b.Property<decimal>("Quantity").HasColumnType("TEXT");
			b.Property<int>("SaleId").HasColumnType("INTEGER");
			b.Property<decimal>("UnitPrice").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("ProductId");
			b.HasIndex("SaleId");
			b.ToTable("SaleItems");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SalePayment", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<decimal>("Amount").HasColumnType("TEXT");
			b.Property<int>("PaymentMethodId").HasColumnType("INTEGER");
			b.Property<int>("SaleId").HasColumnType("INTEGER");
			b.Property<string>("TransactionReference").HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("PaymentMethodId");
			b.HasIndex("SaleId");
			b.ToTable("SalePayment");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SubCategory", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<int>("CategoryId").HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasMaxLength(100)
				.HasColumnType("TEXT");
			b.HasKey("Id");
			b.HasIndex("CategoryId");
			b.ToTable("SubCategory");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.TaxCondition", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("TaxCondition");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.VatRate", delegate(EntityTypeBuilder b)
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
			b.Property<bool>("IsActive").HasColumnType("INTEGER");
			b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
			b.Property<decimal>("Rate").HasColumnType("TEXT");
			b.HasKey("Id");
			b.ToTable("VatRate");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.CashRegister", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.PointOfSale", "PointOfSale").WithMany("CashRegisters").HasForeignKey("PointOfSaleId");
			b.Navigation("PointOfSale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.CashRegisterMovement", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.CashRegister", "CashRegister").WithMany("Movements").HasForeignKey("CashRegisterId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("CashRegister");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Customer", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.TaxCondition", "TaxCondition").WithMany().HasForeignKey("TaxConditionId");
			b.Navigation("TaxCondition");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.ElectronicInvoice", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.PointOfSale", "PointOfSale").WithMany().HasForeignKey("PointOfSaleId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.HasOne("GestionQ.Domain.Entities.Sale", "Sale").WithOne("ElectronicInvoice").HasForeignKey("GestionQ.Domain.Entities.ElectronicInvoice", "SaleId");
			b.Navigation("PointOfSale");
			b.Navigation("Sale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Product", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.SubCategory", "SubCategory").WithMany("Products").HasForeignKey("SubCategoryId");
			b.HasOne("GestionQ.Domain.Entities.VatRate", "VatRate").WithMany().HasForeignKey("VatRateId");
			b.Navigation("SubCategory");
			b.Navigation("VatRate");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.ProductPrice", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.Product", "Product").WithMany("PriceHistory").HasForeignKey("ProductId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("Product");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Sale", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.CashRegister", "CashRegister").WithMany("Sales").HasForeignKey("CashRegisterId");
			b.HasOne("GestionQ.Domain.Entities.Customer", "Customer").WithMany().HasForeignKey("CustomerId");
			b.HasOne("GestionQ.Domain.Entities.PointOfSale", "PointOfSale").WithMany("Sales").HasForeignKey("PointOfSaleId");
			b.Navigation("CashRegister");
			b.Navigation("Customer");
			b.Navigation("PointOfSale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SaleItem", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.Product", "Product").WithMany().HasForeignKey("ProductId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.HasOne("GestionQ.Domain.Entities.Sale", "Sale").WithMany("Items").HasForeignKey("SaleId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("Product");
			b.Navigation("Sale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SalePayment", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.PaymentMethod", "PaymentMethod").WithMany().HasForeignKey("PaymentMethodId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.HasOne("GestionQ.Domain.Entities.Sale", "Sale").WithMany("Payments").HasForeignKey("SaleId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("PaymentMethod");
			b.Navigation("Sale");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SubCategory", delegate(EntityTypeBuilder b)
		{
			b.HasOne("GestionQ.Domain.Entities.Category", "Category").WithMany("SubCategories").HasForeignKey("CategoryId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("Category");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.CashRegister", delegate(EntityTypeBuilder b)
		{
			b.Navigation("Movements");
			b.Navigation("Sales");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Category", delegate(EntityTypeBuilder b)
		{
			b.Navigation("SubCategories");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.PointOfSale", delegate(EntityTypeBuilder b)
		{
			b.Navigation("CashRegisters");
			b.Navigation("Sales");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Product", delegate(EntityTypeBuilder b)
		{
			b.Navigation("PriceHistory");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.Sale", delegate(EntityTypeBuilder b)
		{
			b.Navigation("ElectronicInvoice");
			b.Navigation("Items");
			b.Navigation("Payments");
		});
		modelBuilder.Entity("GestionQ.Domain.Entities.SubCategory", delegate(EntityTypeBuilder b)
		{
			b.Navigation("Products");
		});
	}
}
