using System;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.CajaPOS;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();
		using (LocalDbContext localDbContext = new LocalDbContext())
		{
			localDbContext.Database.EnsureCreated();
			try
			{
				localDbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS SystemSettings (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Key\" TEXT NOT NULL, Value TEXT NULL, Description TEXT NULL);");
				localDbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS OfflineCashRegisters (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, GlobalId TEXT NOT NULL, UserId TEXT NOT NULL, OpeningDate TEXT NOT NULL, ClosingDate TEXT NULL, InitialBalance TEXT NOT NULL, FinalCashBalance TEXT NULL, IsSynced INTEGER NOT NULL, ServerCashRegisterId INTEGER NULL);");
			}
			catch
			{
			}
		}
		Application.Run(new Form1());
	}
}
