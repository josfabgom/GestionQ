using System.Security.Claims;
using GestionQ.Domain.Constants;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.Web.Services
{
    /// <summary>
    /// Ejecuta migraciones, seeding y correcciones históricas en segundo plano
    /// para no bloquear el arranque del Servicio de Windows (evita Error 1053 / timeout de 30s)
    /// y reintenta automáticamente si SQL Server aún está iniciando tras prender la PC.
    /// </summary>
    public class DatabaseInitializationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseInitializationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 20;

            for (int attempt = 1; attempt <= maxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    await InitializeDatabaseAsync(stoppingToken);
                    _logger.LogInformation("Inicialización de base de datos completada en intento {Attempt}.", attempt);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    var delaySeconds = Math.Min(5 * attempt, 30);
                    _logger.LogWarning(ex,
                        "Intento {Attempt}/{MaxAttempts} de inicialización de BD falló. Reintentando en {Delay}s...",
                        attempt, maxAttempts, delaySeconds);

                    LogStartupIssue($"Intento {attempt}/{maxAttempts} falló: {ex.Message}");

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync(stoppingToken);

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF SUSER_ID('NT AUTHORITY\\SYSTEM') IS NOT NULL ALTER SERVER ROLE sysadmin ADD MEMBER [NT AUTHORITY\\SYSTEM];",
                    stoppingToken);
            }
            catch
            {
                // Ignorar si no tiene permisos para modificar roles de servidor
            }

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            var roles = new[] { "Admin", "Vendedor", "Cajero" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }

                if (role == "Admin")
                {
                    var adminRole = await roleManager.FindByNameAsync("Admin");
                    if (adminRole != null)
                    {
                        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
                        var allPermissions = Permissions.GenerateAllPermissions();
                        foreach (var permission in allPermissions)
                        {
                            if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                            {
                                await roleManager.AddClaimAsync(adminRole, new Claim("Permission", permission));
                            }
                        }
                    }
                }
            }

            var adminEmail = "admin@gestionq.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, "Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
            else
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "Admin123");

                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            await DbSeeder.InitializeAsync(dbContext);

            try
            {
                await HistoricCentralCashFix.Run(_serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error corriendo corrección histórica de Caja Central.");
            }
        }

        private static void LogStartupIssue(string message)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "GestionQ_StartupLog.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Nunca fallar por escribir un log
            }
        }
    }
}
