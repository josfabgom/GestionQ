using System;
using System.Linq;
using GestionQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

class Program
{
    static void Main()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(@""Data Source=..\src\GestionQ.Infrastructure\Data\gestionq.db"");
        using (var db = new ApplicationDbContext(optionsBuilder.Options))
        {
            foreach(var pos in db.PointsOfSale.ToList())
            {
                Console.WriteLine($""ID: {pos.Id}, Name: {pos.Name}, Identifier: {pos.PosIdentifier}"");
            }
        }
    }
}
