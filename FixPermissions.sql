USE [GestionQ]
GO

DECLARE @RoleId NVARCHAR(450);
SELECT @RoleId = Id FROM AspNetRoles WHERE NormalizedName = 'ADMIN';

IF @RoleId IS NULL
BEGIN
    SET @RoleId = NEWID();
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RoleId, 'Admin', 'ADMIN', NEWID());
END

-- Borrar los permisos viejos para evitar duplicados si los hubiera
DELETE FROM AspNetRoleClaims WHERE RoleId = @RoleId AND ClaimType = 'Permission';

-- Insertar todos los permisos necesarios para el sistema
INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
VALUES
(@RoleId, 'Permission', 'Permissions.Products.View'),
(@RoleId, 'Permission', 'Permissions.Products.Create'),
(@RoleId, 'Permission', 'Permissions.Products.Edit'),
(@RoleId, 'Permission', 'Permissions.Products.Delete'),
(@RoleId, 'Permission', 'Permissions.Customers.View'),
(@RoleId, 'Permission', 'Permissions.Customers.Create'),
(@RoleId, 'Permission', 'Permissions.Customers.Edit'),
(@RoleId, 'Permission', 'Permissions.Customers.Delete'),
(@RoleId, 'Permission', 'Permissions.Sales.View'),
(@RoleId, 'Permission', 'Permissions.Sales.Create'),
(@RoleId, 'Permission', 'Permissions.Purchases.View'),
(@RoleId, 'Permission', 'Permissions.Purchases.Create'),
(@RoleId, 'Permission', 'Permissions.Purchases.Edit'),
(@RoleId, 'Permission', 'Permissions.Purchases.Delete'),
(@RoleId, 'Permission', 'Permissions.CashRegisters.View'),
(@RoleId, 'Permission', 'Permissions.CashRegisters.Open'),
(@RoleId, 'Permission', 'Permissions.CashRegisters.Close'),
(@RoleId, 'Permission', 'Permissions.CashRegisters.Movement'),
(@RoleId, 'Permission', 'Permissions.Config.View'),
(@RoleId, 'Permission', 'Permissions.Config.Create'),
(@RoleId, 'Permission', 'Permissions.Config.Edit'),
(@RoleId, 'Permission', 'Permissions.Config.Delete'),
(@RoleId, 'Permission', 'Permissions.Config.Manage'),
(@RoleId, 'Permission', 'Permissions.Promotions.View'),
(@RoleId, 'Permission', 'Permissions.Promotions.Create'),
(@RoleId, 'Permission', 'Permissions.Promotions.Edit'),
(@RoleId, 'Permission', 'Permissions.Promotions.Delete'),
(@RoleId, 'Permission', 'Permissions.Users.View'),
(@RoleId, 'Permission', 'Permissions.Users.Create'),
(@RoleId, 'Permission', 'Permissions.Users.Edit'),
(@RoleId, 'Permission', 'Permissions.Users.Delete'),
(@RoleId, 'Permission', 'Permissions.Roles.View'),
(@RoleId, 'Permission', 'Permissions.Roles.Create'),
(@RoleId, 'Permission', 'Permissions.Roles.Edit'),
(@RoleId, 'Permission', 'Permissions.Roles.Delete'),
(@RoleId, 'Permission', 'Permissions.ElectronicInvoices.View'),
(@RoleId, 'Permission', 'Permissions.ElectronicInvoices.Create'),
(@RoleId, 'Permission', 'Permissions.ElectronicInvoices.Edit'),
(@RoleId, 'Permission', 'Permissions.ElectronicInvoices.Delete'),
(@RoleId, 'Permission', 'Permissions.ElectronicInvoices.Manage');

-- Asegurarse de que el usuario admin principal tenga este rol asignado
DECLARE @UserId NVARCHAR(450);
SELECT @UserId = Id FROM AspNetUsers WHERE NormalizedEmail = 'ADMIN@GESTIONQ.COM';

IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
    END
END

PRINT 'Permisos actualizados correctamente para el rol Admin.';
GO
