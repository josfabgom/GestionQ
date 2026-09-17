namespace GestionQ.CajaPOS;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        using (var db = new LocalDbContext()) {
            db.Database.EnsureCreated();
            try {
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "CREATE TABLE IF NOT EXISTS SystemSettings (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Key\" TEXT NOT NULL, Value TEXT NULL, Description TEXT NULL);");
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "CREATE TABLE IF NOT EXISTS OfflineCashRegisters (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, GlobalId TEXT NOT NULL, UserId TEXT NOT NULL, OpeningDate TEXT NOT NULL, ClosingDate TEXT NULL, InitialBalance TEXT NOT NULL, FinalCashBalance TEXT NULL, IsSynced INTEGER NOT NULL, ServerCashRegisterId INTEGER NULL);");
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "CREATE TABLE IF NOT EXISTS ProductPresentations (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Barcode TEXT NOT NULL, IsActive INTEGER NOT NULL, Name TEXT NOT NULL, NeedsLabelPrint INTEGER NOT NULL, Price TEXT NULL, ProductId INTEGER NOT NULL, Quantity TEXT NOT NULL);");
            } catch { }
        }
        
        Application.Run(new Form1());
    }    
}