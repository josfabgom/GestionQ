-- ==========================================================================
--   SCRIPT DE DATOS BÁSICOS E INICIALES PARA GESTIONQ (SQL SERVER)
-- ==========================================================================
USE [GestionQ];
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [AspNetRoles]
-- --------------------------------------------------------------------------
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'bf5ea890-4cf9-4305-bd70-a62578c30d55', N'Admin', N'ADMIN', NULL);
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'5583c32d-5833-4421-8b5a-10c953d8d148', N'Vendedor', N'VENDEDOR', NULL);
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [AspNetUsers]
-- --------------------------------------------------------------------------
INSERT INTO [AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]) VALUES (N'b2714dbe-a91b-4579-9b1e-0a5221f7dc5f', N'admin@gestionq.com', N'ADMIN@GESTIONQ.COM', N'admin@gestionq.com', N'ADMIN@GESTIONQ.COM', 1, N'AQAAAAIAAYagAAAAEFhm/nRFRJv1Et2su5afL+hsh/eiEbqWj+sw9GZmQPdriqQvntWNNK4e965qy1P9UQ==', N'PWR4SNBXX6PUER6DJT3V3HOIWDUCVS5P', N'dddd82fc-fd9c-43a1-becc-a39ba0095381', NULL, 0, 0, NULL, 1, 0);
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [AspNetUserRoles]
-- --------------------------------------------------------------------------
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'b2714dbe-a91b-4579-9b1e-0a5221f7dc5f', N'bf5ea890-4cf9-4305-bd70-a62578c30d55');
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [TaxConditions]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [TaxConditions] ON;
INSERT INTO [TaxConditions] ([Id], [Name]) VALUES (1, N'Responsable Inscripto');
INSERT INTO [TaxConditions] ([Id], [Name]) VALUES (2, N'Monotributista');
INSERT INTO [TaxConditions] ([Id], [Name]) VALUES (3, N'Exento');
INSERT INTO [TaxConditions] ([Id], [Name]) VALUES (4, N'Consumidor Final');
SET IDENTITY_INSERT [TaxConditions] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [VatRates]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [VatRates] ON;
INSERT INTO [VatRates] ([Id], [Name], [Rate]) VALUES (1, N'IVA 21%', 21.00);
INSERT INTO [VatRates] ([Id], [Name], [Rate]) VALUES (2, N'IVA 10.5%', 10.50);
INSERT INTO [VatRates] ([Id], [Name], [Rate]) VALUES (3, N'IVA 0%', 0.00);
SET IDENTITY_INSERT [VatRates] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [Categories]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [Name]) VALUES (1, N'Bebidas');
INSERT INTO [Categories] ([Id], [Name]) VALUES (2, N'Comestibles');
INSERT INTO [Categories] ([Id], [Name]) VALUES (3, N'Limpieza');
SET IDENTITY_INSERT [Categories] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [SubCategories]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [SubCategories] ON;
INSERT INTO [SubCategories] ([Id], [Name], [CategoryId]) VALUES (1, N'Gaseosas', 1);
INSERT INTO [SubCategories] ([Id], [Name], [CategoryId]) VALUES (2, N'Alcohol', 1);
INSERT INTO [SubCategories] ([Id], [Name], [CategoryId]) VALUES (3, N'Galletitas', 2);
INSERT INTO [SubCategories] ([Id], [Name], [CategoryId]) VALUES (4, N'Almacén', 2);
INSERT INTO [SubCategories] ([Id], [Name], [CategoryId]) VALUES (5, N'Cuidado Personal', 3);
SET IDENTITY_INSERT [SubCategories] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [PaymentMethods]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [PaymentMethods] ON;
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (1, N'Efectivo', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (2, N'Tarjeta de Crédito', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (3, N'Tarjeta de Débito', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (4, N'Transferencia', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (5, N'Cuenta Corriente', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (6, N'Mercado Pago QR', 1);
INSERT INTO [PaymentMethods] ([Id], [Name], [IsActive]) VALUES (7, N'Mercado Pago Point', 1);
SET IDENTITY_INSERT [PaymentMethods] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [Customers]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [Customers] ON;
INSERT INTO [Customers] ([Id], [Name], [Email], [Phone], [Balance], [IsActive], [DiscountPercentage], [TaxConditionId]) VALUES (1, N'Juan Pérez', N'juan.perez@email.com', N'1133445566', 0.0, 1, 0.00, 4);
INSERT INTO [Customers] ([Id], [Name], [Email], [Phone], [Balance], [IsActive], [DiscountPercentage], [TaxConditionId]) VALUES (2, N'María Gómez', N'maria.gomez@email.com', N'1199887766', 5000.0, 1, 0.00, 4);
INSERT INTO [Customers] ([Id], [Name], [Email], [Phone], [Balance], [IsActive], [DiscountPercentage], [TaxConditionId]) VALUES (3, N'Carlos López', N'carlos.lopez@email.com', N'1122334455', -1500.0, 1, 0.00, 4);
INSERT INTO [Customers] ([Id], [Name], [Email], [Phone], [Balance], [IsActive], [DiscountPercentage], [TaxConditionId]) VALUES (4, N'Ana Martínez', N'ana.martinez@email.com', N'1155443322', 0.0, 1, 0.00, 4);
INSERT INTO [Customers] ([Id], [Name], [Email], [Phone], [Balance], [IsActive], [DiscountPercentage], [TaxConditionId]) VALUES (5, N'Empresa ABC', N'compras@abc.com', N'1166778899', 15000.0, 1, 0.00, 4);
SET IDENTITY_INSERT [Customers] OFF;
GO

-- --------------------------------------------------------------------------
-- Datos para la tabla [Products]
-- --------------------------------------------------------------------------
SET IDENTITY_INSERT [Products] ON;
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (1, 1, N'779123456001', N'Coca Cola 1.5L', 1, 0, 0, 2500.0, 149, 0.00, 1, N'2026-03-07 07:26:13.6114102', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (2, 2, N'779123456002', N'Sprite 1.5L', 1, 0, 0, 2500.0, 80, 0.00, 1, N'2026-03-07 07:26:13.6133616', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (3, 3, N'779123456003', N'Fanta 1.5L', 1, 0, 0, 2500.0, 47, 0.00, 1, N'2026-03-07 07:26:13.613363', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (4, 4, N'779123456004', N'Galletas Oreo 117g', 3, 0, 0, 1200.0, 197, 0.00, 1, N'2026-03-07 07:26:13.6133632', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (5, 5, N'779123456005', N'Chocolinas 250g', 3, 0, 0, 1500.0, 120, 0.00, 1, N'2026-03-07 07:26:13.6133633', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (6, 6, N'779123456006', N'Yerba Mate Playadito 1Kg', 4, 0, 0, 4500.0, 60, 0.00, 1, N'2026-03-07 07:26:13.6133641', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (7, 7, N'779123456007', N'Yerba Mate Taragüi 1Kg', 4, 0, 0, 4200.0, 43, 0.00, 1, N'2026-03-07 07:26:13.6133642', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (8, 8, N'779123456008', N'Azúcar Ledesma 1Kg', 4, 0, 0, 900.0, 297, 0.00, 1, N'2026-03-07 07:26:13.6133643', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (9, 9, N'779123456009', N'Fideos Matarazzo 500g', 4, 0, 0, 1100.0, 150, 0.00, 1, N'2026-03-07 07:26:13.6133644', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (10, 10, N'779123456010', N'Arroz Gallo Oro 1Kg', 4, 0, 0, 1600.0, 100, 0.00, 1, N'2026-03-07 07:26:13.6133647', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (11, 11, N'779123456011', N'Leche La Serenísima 1L', 4, 0, 0, 1300.0, 249, 0.00, 1, N'2026-03-07 07:26:13.6133648', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (12, 12, N'779123456012', N'Queso Cremoso La Paulina 1Kg', 4, 0, 0, 6500.0, 28, 0.00, 1, N'2026-03-07 07:26:13.6133649', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (13, 13, N'779123456013', N'Manteca Sancor 200g', 4, 0, 0, 2200.0, 65, 0.00, 1, N'2026-03-07 07:26:13.6133651', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (14, 14, N'779123456014', N'Cerveza Quilmes 1L', 2, 0, 0, 2800.0, 180, 0.00, 1, N'2026-03-07 07:26:13.6133652', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (15, 15, N'779123456015', N'Vino Rutini Malbec 750ml', 2, 0, 0, 12000.0, 40, 0.00, 1, N'2026-03-07 07:26:13.6133654', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (16, 16, N'779123456016', N'Papel Higiénico Scott', 5, 0, 0, 2500.0, 82, 0.00, 1, N'2026-03-07 07:26:13.6133655', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (17, 17, N'779123456017', N'Rollo de Cocina Sussex', 5, 0, 0, 1800.0, 95, 0.00, 1, N'2026-03-07 07:26:13.6133657', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (18, 18, N'779123456018', N'Lavandina Ayudín 1L', 5, 0, 0, 950.0, 110, 0.00, 1, N'2026-03-07 07:26:13.6133659', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (19, 19, N'779123456019', N'Detergente Magistral 500ml', 5, 0, 0, 1700.0, 140, 0.00, 1, N'2026-03-07 07:26:13.613366', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (20, 20, N'779123456020', N'Desodorante Rexona Aerosol', 5, 0, 0, 2900.0, 70, 0.00, 1, N'2026-03-07 07:26:13.6133661', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (21, 21, N'779123456021', N'Shampoo Pantene 400ml', 5, 0, 0, 3500.0, 60, 0.00, 1, N'2026-03-07 07:26:13.6133663', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (22, 22, N'779123456022', N'Jabón Tocador Rexona (pack 3)', 5, 0, 0, 2100.0, 90, 0.00, 1, N'2026-03-07 07:26:13.6133664', 0, 1);
INSERT INTO [Products] ([Id], [InternalCode], [Barcode], [Name], [SubCategoryId], [IsPesable], [SendToScale], [Price], [Stock], [MinimumStock], [VatRateId], [CreationDate], [ExpirationDays], [IsActive]) VALUES (23, 23, N'779123456023', N'Gaseosa Inactiva (Discontinuado)', 1, 0, 0, 1000.0, 5, 0.00, 1, N'2026-03-07 07:26:13.6133665', 0, 0);
SET IDENTITY_INSERT [Products] OFF;
GO
