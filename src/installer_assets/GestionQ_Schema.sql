IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Balance] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);

CREATE TABLE [PaymentMethods] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [InternalCode] int NOT NULL,
    [Barcode] nvarchar(50) NULL,
    [Name] nvarchar(100) NOT NULL,
    [Category] nvarchar(100) NULL,
    [Price] decimal(18,2) NOT NULL,
    [Stock] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CashRegisters] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [OpeningDate] datetime2 NOT NULL,
    [ClosingDate] datetime2 NULL,
    [InitialBalance] decimal(18,2) NOT NULL,
    [ExpectedCashBalance] decimal(18,2) NULL,
    [FinalCashBalance] decimal(18,2) NULL,
    [Difference] decimal(18,2) NULL,
    CONSTRAINT [PK_CashRegisters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashRegisters_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Sales] (
    [Id] int NOT NULL IDENTITY,
    [Date] datetime2 NOT NULL,
    [CustomerId] int NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [UserId] nvarchar(450) NULL,
    [CashRegisterId] int NULL,
    CONSTRAINT [PK_Sales] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sales_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Sales_CashRegisters_CashRegisterId] FOREIGN KEY ([CashRegisterId]) REFERENCES [CashRegisters] ([Id]),
    CONSTRAINT [FK_Sales_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id])
);

CREATE TABLE [SaleItems] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_SaleItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SaleItems_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SalePayments] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [PaymentMethodId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_SalePayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalePayments_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalePayments_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_CashRegisters_UserId] ON [CashRegisters] ([UserId]);

CREATE INDEX [IX_SaleItems_ProductId] ON [SaleItems] ([ProductId]);

CREATE INDEX [IX_SaleItems_SaleId] ON [SaleItems] ([SaleId]);

CREATE INDEX [IX_SalePayments_PaymentMethodId] ON [SalePayments] ([PaymentMethodId]);

CREATE INDEX [IX_SalePayments_SaleId] ON [SalePayments] ([SaleId]);

CREATE INDEX [IX_Sales_CashRegisterId] ON [Sales] ([CashRegisterId]);

CREATE INDEX [IX_Sales_CustomerId] ON [Sales] ([CustomerId]);

CREATE INDEX [IX_Sales_UserId] ON [Sales] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260308123648_SqlServerMigration', N'9.0.15');

ALTER TABLE [Customers] ADD [Address] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [Dni] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [ImageUrl] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [InternalCode] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Customers] ADD [Locality] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425090414_AddCustomerFields', N'9.0.15');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Category');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Products] DROP COLUMN [Category];

ALTER TABLE [Products] ADD [SubCategoryId] int NULL;

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [SubCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [CategoryId] int NOT NULL,
    CONSTRAINT [PK_SubCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Products_SubCategoryId] ON [Products] ([SubCategoryId]);

CREATE INDEX [IX_SubCategories_CategoryId] ON [SubCategories] ([CategoryId]);

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_SubCategories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [SubCategories] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425091829_AddRubrosSubrubros', N'9.0.15');

ALTER TABLE [Products] ADD [IsPesable] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [SendToScale] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425092310_AddScaleFields', N'9.0.15');

ALTER TABLE [Products] ADD [ImageUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425092604_AddProductImage', N'9.0.15');

CREATE TABLE [StockMovements] (
    [Id] int NOT NULL IDENTITY,
    [Date] datetime2 NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Type] int NOT NULL,
    [Concept] nvarchar(max) NULL,
    [PurchaseId] int NULL,
    [SaleId] int NULL,
    [PreviousStock] decimal(18,2) NOT NULL,
    [NewStock] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [TaxId] nvarchar(20) NULL,
    [Address] nvarchar(100) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);

CREATE TABLE [Purchases] (
    [Id] int NOT NULL IDENTITY,
    [Date] datetime2 NOT NULL,
    [SupplierId] int NOT NULL,
    [ReferenceNumber] nvarchar(50) NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PurchaseItems] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PurchaseItems_ProductId] ON [PurchaseItems] ([ProductId]);

CREATE INDEX [IX_PurchaseItems_PurchaseId] ON [PurchaseItems] ([PurchaseId]);

CREATE INDEX [IX_Purchases_SupplierId] ON [Purchases] ([SupplierId]);

CREATE INDEX [IX_StockMovements_ProductId] ON [StockMovements] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425093211_AddStockManagement', N'9.0.15');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Stock');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Products] ALTER COLUMN [Stock] decimal(18,2) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425093703_ChangeStockToDecimal', N'9.0.15');

ALTER TABLE [Suppliers] ADD [City] nvarchar(100) NULL;

ALTER TABLE [Suppliers] ADD [ContactPerson] nvarchar(100) NULL;

ALTER TABLE [Suppliers] ADD [VatConditionId] int NULL;

CREATE TABLE [VatConditions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_VatConditions] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Suppliers_VatConditionId] ON [Suppliers] ([VatConditionId]);

ALTER TABLE [Suppliers] ADD CONSTRAINT [FK_Suppliers_VatConditions_VatConditionId] FOREIGN KEY ([VatConditionId]) REFERENCES [VatConditions] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425094325_AddVatConditions', N'9.0.15');

ALTER TABLE [Suppliers] DROP CONSTRAINT [FK_Suppliers_VatConditions_VatConditionId];

DROP TABLE [VatConditions];

EXEC sp_rename N'[Suppliers].[VatConditionId]', N'TaxConditionId', 'COLUMN';

EXEC sp_rename N'[Suppliers].[IX_Suppliers_VatConditionId]', N'IX_Suppliers_TaxConditionId', 'INDEX';

ALTER TABLE [Products] ADD [VatRateId] int NULL;

CREATE TABLE [TaxConditions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TaxConditions] PRIMARY KEY ([Id])
);

CREATE TABLE [VatRates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_VatRates] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Products_VatRateId] ON [Products] ([VatRateId]);

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_VatRates_VatRateId] FOREIGN KEY ([VatRateId]) REFERENCES [VatRates] ([Id]);

ALTER TABLE [Suppliers] ADD CONSTRAINT [FK_Suppliers_TaxConditions_TaxConditionId] FOREIGN KEY ([TaxConditionId]) REFERENCES [TaxConditions] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425095310_SplitTaxEntities', N'9.0.15');

ALTER TABLE [Purchases] ADD [ImageUrl] nvarchar(max) NULL;

ALTER TABLE [Purchases] ADD [VoucherLetter] nvarchar(1) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425100021_AddPurchaseImageAndLetter', N'9.0.15');

ALTER TABLE [Products] ADD [BaseCost] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Products] ADD [InternalTax] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Products] ADD [ProfitMargin] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425101623_AddProductPricingFields', N'9.0.15');

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'BaseCost');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Products] DROP COLUMN [BaseCost];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'InternalTax');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Products] DROP COLUMN [InternalTax];

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ProfitMargin');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Products] DROP COLUMN [ProfitMargin];

CREATE TABLE [ProductPrices] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [BaseCost] decimal(18,2) NOT NULL,
    [ProfitMargin] decimal(18,2) NOT NULL,
    [InternalTax] decimal(18,2) NOT NULL,
    [FinalPrice] decimal(18,2) NOT NULL,
    [UpdateDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductPrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductPrices_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ProductPrices_ProductId] ON [ProductPrices] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425102454_AddProductPriceHistory', N'9.0.15');

CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425110109_AddSystemSettings', N'9.0.15');

ALTER TABLE [Customers] ADD [Cuit] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [TaxConditionId] int NULL;

CREATE INDEX [IX_Customers_TaxConditionId] ON [Customers] ([TaxConditionId]);

ALTER TABLE [Customers] ADD CONSTRAINT [FK_Customers_TaxConditions_TaxConditionId] FOREIGN KEY ([TaxConditionId]) REFERENCES [TaxConditions] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501094828_AddCuitAndTaxConditionToCustomer', N'9.0.15');

ALTER TABLE [Purchases] ADD [Status] int NOT NULL DEFAULT 0;

ALTER TABLE [PurchaseItems] ADD [ReceivedQuantity] decimal(18,2) NULL;

ALTER TABLE [Products] ADD [MinimumStock] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501095813_AddMinimumStockAndPurchaseStatus', N'9.0.15');

ALTER TABLE [Products] ADD [ExpirationDays] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515091427_AddExpirationDaysToProduct', N'9.0.15');

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SaleItems]') AND [c].[name] = N'Quantity');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [SaleItems] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [SaleItems] ALTER COLUMN [Quantity] decimal(18,2) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515091742_FixSaleItemQuantityType', N'9.0.15');

ALTER TABLE [Sales] ADD [PointOfSaleId] int NULL;

ALTER TABLE [CashRegisters] ADD [PointOfSaleId] int NULL;

CREATE TABLE [PointsOfSale] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PointsOfSale] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Sales_PointOfSaleId] ON [Sales] ([PointOfSaleId]);

CREATE INDEX [IX_CashRegisters_PointOfSaleId] ON [CashRegisters] ([PointOfSaleId]);

ALTER TABLE [CashRegisters] ADD CONSTRAINT [FK_CashRegisters_PointsOfSale_PointOfSaleId] FOREIGN KEY ([PointOfSaleId]) REFERENCES [PointsOfSale] ([Id]);

ALTER TABLE [Sales] ADD CONSTRAINT [FK_Sales_PointsOfSale_PointOfSaleId] FOREIGN KEY ([PointOfSaleId]) REFERENCES [PointsOfSale] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260516101836_AddPointsOfSale', N'9.0.15');

ALTER TABLE [PointsOfSale] ADD [MachineName] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260516102919_AddMachineNameToPOS', N'9.0.15');

ALTER TABLE [PointsOfSale] ADD [PosNumber] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260516104448_AddPosNumberToPOS', N'9.0.15');

CREATE TABLE [ElectronicInvoices] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NULL,
    [PointOfSaleId] int NOT NULL,
    [PointOfSaleNumber] int NOT NULL,
    [InvoiceTypeCode] int NOT NULL,
    [InvoiceTypeDesc] nvarchar(max) NOT NULL,
    [InvoiceNumber] int NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [ConceptCode] int NOT NULL,
    [DocTypeCode] int NOT NULL,
    [DocNumber] nvarchar(max) NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [CustomerTaxCondition] nvarchar(max) NOT NULL,
    [NetAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [ExemptAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [CAE] nvarchar(max) NOT NULL,
    [CAEExpirationDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CanMisMonExt] bit NOT NULL,
    [CondicionIVAReceptorId] int NOT NULL,
    CONSTRAINT [PK_ElectronicInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ElectronicInvoices_PointsOfSale_PointOfSaleId] FOREIGN KEY ([PointOfSaleId]) REFERENCES [PointsOfSale] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ElectronicInvoices_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE SET NULL
);

CREATE INDEX [IX_ElectronicInvoices_PointOfSaleId] ON [ElectronicInvoices] ([PointOfSaleId]);

CREATE UNIQUE INDEX [IX_ElectronicInvoices_SaleId] ON [ElectronicInvoices] ([SaleId]) WHERE [SaleId] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260518223043_AddElectronicInvoicing', N'9.0.15');

CREATE TABLE [FiscalPrintJobs] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [PrintedDate] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [PrintedCount] int NOT NULL,
    [PrinterName] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FiscalPrintJobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FiscalPrintJobs_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_FiscalPrintJobs_SaleId] ON [FiscalPrintJobs] ([SaleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520084603_AddFiscalPrintSpooler', N'9.0.15');

CREATE TABLE [CashRegisterMovements] (
    [Id] int NOT NULL IDENTITY,
    [CashRegisterId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Date] datetime2 NOT NULL,
    CONSTRAINT [PK_CashRegisterMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashRegisterMovements_CashRegisters_CashRegisterId] FOREIGN KEY ([CashRegisterId]) REFERENCES [CashRegisters] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_CashRegisterMovements_CashRegisterId] ON [CashRegisterMovements] ([CashRegisterId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260524102013_AddCashRegisterMovements', N'9.0.15');

ALTER TABLE [Products] ADD [LastSentToScaleDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260610021043_AddLastSentToScaleDate', N'9.0.15');

CREATE TABLE [MercadoPagoConfigs] (
    [Id] int NOT NULL IDENTITY,
    [PointOfSaleId] int NULL,
    [AccessToken] nvarchar(255) NOT NULL,
    [ExternalPosId] nvarchar(100) NOT NULL,
    [PointDeviceId] nvarchar(100) NOT NULL,
    [DefaultMethod] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MercadoPagoConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MercadoPagoConfigs_PointsOfSale_PointOfSaleId] FOREIGN KEY ([PointOfSaleId]) REFERENCES [PointsOfSale] ([Id])
);

CREATE INDEX [IX_MercadoPagoConfigs_PointOfSaleId] ON [MercadoPagoConfigs] ([PointOfSaleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260613100854_AddMercadoPagoConfig', N'9.0.15');


                IF NOT EXISTS (SELECT 1 FROM PaymentMethods WHERE Name = 'Mercado Pago')
                BEGIN
                    INSERT INTO PaymentMethods (Name, IsActive) VALUES ('Mercado Pago', 1);
                END
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260613102852_SeedMercadoPagoMethod', N'9.0.15');

ALTER TABLE [SalePayments] ADD [TransactionReference] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260614011503_AddTransactionReferenceToPayments', N'9.0.15');

ALTER TABLE [Sales] ADD [CancellationDate] datetime2 NULL;

ALTER TABLE [Sales] ADD [IsCancelled] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260614020100_AddSaleCancellationFields', N'9.0.15');

ALTER TABLE [Sales] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Sales] ADD [SubTotal] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [SaleItems] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Customers] ADD [DiscountPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE [PromotionRules] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Type] int NOT NULL,
    [Value] decimal(18,2) NOT NULL,
    [BuyQuantity] int NULL,
    [PayQuantity] int NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PromotionRules] PRIMARY KEY ([Id])
);

CREATE TABLE [PromotionRuleProducts] (
    [PromotionRuleId] int NOT NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_PromotionRuleProducts] PRIMARY KEY ([PromotionRuleId], [ProductId]),
    CONSTRAINT [FK_PromotionRuleProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionRuleProducts_PromotionRules_PromotionRuleId] FOREIGN KEY ([PromotionRuleId]) REFERENCES [PromotionRules] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PromotionRuleProducts_ProductId] ON [PromotionRuleProducts] ([ProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615100426_AddDiscountAndPromotionSystem', N'9.0.15');

ALTER TABLE [Sales] ADD [PaymentDiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PromotionRules] ADD [IsStackable] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [PaymentMethods] ADD [DiscountPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615104737_AddStackablePromotionsAndPaymentDiscounts', N'9.0.15');

ALTER TABLE [MercadoPagoConfigs] ADD [ExpiresAt] datetime2 NULL;

ALTER TABLE [MercadoPagoConfigs] ADD [MpUserId] bigint NULL;

ALTER TABLE [MercadoPagoConfigs] ADD [RefreshToken] nvarchar(255) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260616020222_AddMercadoPagoOAuthFields', N'9.0.15');

ALTER TABLE [SaleItems] ADD [CustomName] nvarchar(150) NULL;

ALTER TABLE [Products] ADD [IsDepartment] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Hotkey] nvarchar(10) NULL,
    [VatRateId] int NOT NULL,
    [VirtualProductId] int NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_Products_VirtualProductId] FOREIGN KEY ([VirtualProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Departments_VatRates_VatRateId] FOREIGN KEY ([VatRateId]) REFERENCES [VatRates] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Departments_VatRateId] ON [Departments] ([VatRateId]);

CREATE INDEX [IX_Departments_VirtualProductId] ON [Departments] ([VirtualProductId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260619021826_AddDepartmentsAndCustomSaleItemName', N'9.0.15');

ALTER TABLE [Products] ADD [NeedsLabelPrint] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260621020239_AddNeedsLabelPrint', N'9.0.15');

ALTER TABLE [Products] ADD [IsFractionable] bit NOT NULL DEFAULT CAST(0 AS bit);

UPDATE Products SET IsFractionable = 1 WHERE IsPesable = 1

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260624024200_AddIsFractionableToProduct', N'9.0.15');

ALTER TABLE [PointsOfSale] ADD [PrinterName] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260626082413_AddPrinterNameToPos', N'9.0.15');

ALTER TABLE [PointsOfSale] ADD [PrintCopies] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260626090146_AddPrintCopiesToPointOfSale', N'9.0.15');

ALTER TABLE [Products] ADD [SupplierCode] nvarchar(50) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260703021041_AddSupplierCode', N'9.0.15');

COMMIT;
GO

