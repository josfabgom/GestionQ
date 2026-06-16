namespace GestionQ.Domain.Constants
{
    public static class Permissions
    {
        public static class Products
        {
            public const string View = "Permissions.Products.View";
            public const string Create = "Permissions.Products.Create";
            public const string Edit = "Permissions.Products.Edit";
            public const string Delete = "Permissions.Products.Delete";
        }

        public static class Customers
        {
            public const string View = "Permissions.Customers.View";
            public const string Create = "Permissions.Customers.Create";
            public const string Edit = "Permissions.Customers.Edit";
            public const string Delete = "Permissions.Customers.Delete";
        }

        public static class Sales
        {
            public const string View = "Permissions.Sales.View";
            public const string Create = "Permissions.Sales.Create";
        }

        public static class Purchases
        {
            public const string View = "Permissions.Purchases.View";
            public const string Create = "Permissions.Purchases.Create";
            public const string Edit = "Permissions.Purchases.Edit";
            public const string Delete = "Permissions.Purchases.Delete";
        }

        public static class CashRegisters
        {
            public const string View = "Permissions.CashRegisters.View";
            public const string Open = "Permissions.CashRegisters.Open";
            public const string Close = "Permissions.CashRegisters.Close";
            public const string Movement = "Permissions.CashRegisters.Movement";
        }

        public static class Config
        {
            public const string View = "Permissions.Config.View";
            public const string Create = "Permissions.Config.Create";
            public const string Edit = "Permissions.Config.Edit";
            public const string Delete = "Permissions.Config.Delete";
            public const string Manage = "Permissions.Config.Manage";
        }

        public static class Promotions
        {
            public const string View = "Permissions.Promotions.View";
            public const string Create = "Permissions.Promotions.Create";
            public const string Edit = "Permissions.Promotions.Edit";
            public const string Delete = "Permissions.Promotions.Delete";
        }

        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
            public const string Delete = "Permissions.Users.Delete";
        }

        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string Create = "Permissions.Roles.Create";
            public const string Edit = "Permissions.Roles.Edit";
            public const string Delete = "Permissions.Roles.Delete";
        }

        public static class ElectronicInvoices
        {
            public const string View = "Permissions.ElectronicInvoices.View";
            public const string Create = "Permissions.ElectronicInvoices.Create";
            public const string Edit = "Permissions.ElectronicInvoices.Edit";
            public const string Delete = "Permissions.ElectronicInvoices.Delete";
            public const string Manage = "Permissions.ElectronicInvoices.Manage";
        }

        /// <summary>
        /// Returns a list of all available permissions in the system.
        /// </summary>
        public static List<string> GenerateAllPermissions()
        {
            var permissions = new List<string>();

            // Products
            permissions.Add(Products.View);
            permissions.Add(Products.Create);
            permissions.Add(Products.Edit);
            permissions.Add(Products.Delete);

            // Customers
            permissions.Add(Customers.View);
            permissions.Add(Customers.Create);
            permissions.Add(Customers.Edit);
            permissions.Add(Customers.Delete);

            // Sales
            permissions.Add(Sales.View);
            permissions.Add(Sales.Create);

            // Purchases
            permissions.Add(Purchases.View);
            permissions.Add(Purchases.Create);
            permissions.Add(Purchases.Edit);
            permissions.Add(Purchases.Delete);

            // Cash Registers
            permissions.Add(CashRegisters.View);
            permissions.Add(CashRegisters.Open);
            permissions.Add(CashRegisters.Close);
            permissions.Add(CashRegisters.Movement);

            // Configuration
            permissions.Add(Config.View);
            permissions.Add(Config.Create);
            permissions.Add(Config.Edit);
            permissions.Add(Config.Delete);
            permissions.Add(Config.Manage);

            // Promotions
            permissions.Add(Promotions.View);
            permissions.Add(Promotions.Create);
            permissions.Add(Promotions.Edit);
            permissions.Add(Promotions.Delete);

            // Users
            permissions.Add(Users.View);
            permissions.Add(Users.Create);
            permissions.Add(Users.Edit);
            permissions.Add(Users.Delete);

            // Roles
            permissions.Add(Roles.View);
            permissions.Add(Roles.Create);
            permissions.Add(Roles.Edit);
            permissions.Add(Roles.Delete);

            // Electronic Invoices
            permissions.Add(ElectronicInvoices.View);
            permissions.Add(ElectronicInvoices.Create);
            permissions.Add(ElectronicInvoices.Edit);
            permissions.Add(ElectronicInvoices.Delete);
            permissions.Add(ElectronicInvoices.Manage);

            return permissions;
        }
    }
}
