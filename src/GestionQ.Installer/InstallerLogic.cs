using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace GestionQ.Installer
{
    public class InstallerLogic
    {
        private const string ServiceName = "GestionQ_Web_Service";
        private const string ServiceDisplayName = "GestionQ Web Server";
        private const string ServiceDescription = "Servidor backend central para el sistema de GestionQ.";

        public event Action<string, double>? OnProgressChanged;
        public event Action<string>? OnError;

        private void ReportProgress(string status, double percent)
        {
            OnProgressChanged?.Invoke(status, percent);
        }

        private void LogMessage(string message)
        {
            // Simple helper for logging
            Debug.WriteLine(message);
        }

        public void RunInstall(string installDir)
        {
            try
            {
                ReportProgress("Iniciando proceso...", 10);

                // 1. Stop existing services/processes if any
                ReportProgress("Deteniendo servicios en ejecución...", 20);
                StopService();
                try
                {
                    var processes = Process.GetProcessesByName("GestionQ.ServerMonitor");
                    foreach (var p in processes) { p.Kill(); p.WaitForExit(2000); }
                }
                catch { }

                // 2. Extract Embedded Resources
                ReportProgress("Extrayendo archivos...", 40);
                ExtractResources(installDir, isUpdate: false);

                // 3. Install SQL Server if needed
                string sqlExePath = Path.Combine(installDir, "SQL2022-SSEI-Expr.exe");
                if (File.Exists(sqlExePath))
                {
                    ReportProgress("Instalando SQL Server Express (puede tardar varios minutos)...", 50);
                    try
                    {
                        RunProcess(sqlExePath, "/Q /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSVCACCOUNT=\"NT AUTHORITY\\Network Service\" /SQLSYSADMINACCOUNTS=\"BUILTIN\\Administrators\" /AGTSVCACCOUNT=\"NT AUTHORITY\\Network Service\" /IACCEPTSQLSERVERLICENSETERMS");
                    }
                    catch (Exception ex)
                    {
                        // Some systems might have pending reboots or existing installs that fail the bootstrapper
                        LogMessage("Advertencia al instalar SQL Server: " + ex.Message);
                    }
                }

                ReportProgress("Configurando base de datos local...", 60);
                SetupDatabase(installDir);

                // 4. Install Service
                ReportProgress("Instalando Servicio de Windows...", 80);
                InstallService(installDir);

                // 5. Configure Firewall
                ReportProgress("Abriendo puertos en el Firewall...", 90);
                ConfigureFirewall();

                // 6. Start Service
                ReportProgress("Iniciando servicio...", 95);
                StartService();

                // 7. Create shortcuts and start Server Monitor
                ReportProgress("Creando accesos directos e iniciando Monitor...", 98);
                string shortcutsScript = Path.Combine(installDir, "scripts", "launcher", "crear_accesos.ps1");
                if (File.Exists(shortcutsScript))
                {
                    RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{shortcutsScript}\"");
                }
                
                string monitorPath = Path.Combine(installDir, "app", "GestionQ.ServerMonitor.exe");
                if (File.Exists(monitorPath))
                {
                    Process.Start(new ProcessStartInfo { FileName = monitorPath, UseShellExecute = true });
                }

                ReportProgress("¡Instalación completada!", 100);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public void RunUpdate(string installDir)
        {
            try
            {
                ReportProgress("Iniciando actualización...", 10);
                
                StopService();
                try
                {
                    var processes = Process.GetProcessesByName("GestionQ.ServerMonitor");
                    foreach (var p in processes) { p.Kill(); p.WaitForExit(2000); }
                }
                catch { }

                ReportProgress("Actualizando archivos...", 50);
                ExtractResources(installDir, isUpdate: true);

                ReportProgress("Base de datos será actualizada al iniciar el servicio...", 60);
                // SetupDatabase(installDir); // EF Core automaticamente migra al arrancar

                ReportProgress("Verificando Servicio de Windows...", 70);
                InstallService(installDir);

                ReportProgress("Actualizando accesos directos...", 80);
                string shortcutsScript = Path.Combine(installDir, "scripts", "launcher", "crear_accesos.ps1");
                if (File.Exists(shortcutsScript))
                {
                    RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{shortcutsScript}\"");
                }

                ReportProgress("Reiniciando servicio...", 95);
                StartService();
                
                string monitorPath = Path.Combine(installDir, "app", "GestionQ.ServerMonitor.exe");
                if (File.Exists(monitorPath))
                {
                    Process.Start(new ProcessStartInfo { FileName = monitorPath, UseShellExecute = true });
                }

                ReportProgress("¡Actualización completada!", 100);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        private void ExtractResources(string targetDir, bool isUpdate)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Extract App payload
            string payloadZip = Path.Combine(targetDir, "payload.zip");
            ExtractSingleResource(assembly, "GestionQ.Installer.Resources.payload.zip", payloadZip);

            if (File.Exists(payloadZip))
            {
                // Unzip over existing
                using (ZipArchive archive = ZipFile.OpenRead(payloadZip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // It's a directory

                        if (isUpdate && entry.Name.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip overriding config on update
                        }

                        string destPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                }
                File.Delete(payloadZip);
            }

            // Always extract SQL Scripts for idempotent execution
            ExtractSingleResource(assembly, "GestionQ.Installer.Resources.GestionQ_Schema.sql", Path.Combine(targetDir, "GestionQ_Schema.sql"));
            ExtractSingleResource(assembly, "GestionQ.Installer.Resources.GestionQ_Datos_Basicos.sql", Path.Combine(targetDir, "GestionQ_Datos_Basicos.sql"));

            if (!isUpdate)
            {
                // Extract SQL Installer if exists
                ExtractSingleResource(assembly, "GestionQ.Installer.Resources.SQL2022-SSEI-Expr.exe", Path.Combine(targetDir, "SQL2022-SSEI-Expr.exe"));
            }
        }

        private void ExtractSingleResource(Assembly assembly, string resourceName, string destPath)
        {
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var fileStream = File.Create(destPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
        }

        private void SetupDatabase(string installDir)
        {
            string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
            string schemaPath = Path.Combine(installDir, "GestionQ_Schema.sql");
            string dataPath = Path.Combine(installDir, "GestionQ_Datos_Basicos.sql");

            if (!File.Exists(schemaPath)) return;

            // Wait for SQL Server to be ready
            bool isReady = false;
            for (int i = 0; i < 12; i++) // Try for 1 minute
            {
                try
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        isReady = true;
                        break;
                    }
                }
                catch
                {
                    Thread.Sleep(5000);
                }
            }

            if (!isReady)
            {
                throw new Exception("El servidor SQL no respondió después de la instalación.");
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string createDb = @"
                    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'gestionq')
                    BEGIN
                        CREATE DATABASE [gestionq];
                    END";
                using (var cmd = new SqlCommand(createDb, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // Execute schema and data scripts
            Action<string> executeScript = (path) =>
            {
                if (!File.Exists(path)) return;
                string script = File.ReadAllText(path);
                string[] batches = System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                using (var c = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=gestionq;Trusted_Connection=True;TrustServerCertificate=True;"))
                {
                    c.Open();
                    foreach (string batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;
                        using (var cmd = new SqlCommand(batch, c)) { cmd.ExecuteNonQuery(); }
                    }
                }
            };
            
            executeScript(schemaPath);
            executeScript(dataPath);
        }

        private void InstallService(string installDir)
        {
            StopService();
            RunProcess("sc.exe", $"delete {ServiceName}", ignoreErrors: true);
            Thread.Sleep(2000);

            string exePath = Path.Combine(installDir, "app", "GestionQ.Web.exe");
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"No se encontró el ejecutable del servicio web en {exePath}");
            }

            RunProcess("sc.exe", $"create {ServiceName} binPath= \"{exePath}\" start= auto DisplayName= \"{ServiceDisplayName}\"");
            RunProcess("sc.exe", $"description {ServiceName} \"{ServiceDescription}\"");
        }

        private void ConfigureFirewall()
        {
            RunProcess("netsh.exe", "advfirewall firewall add rule name=\"GestionQ Web App (TCP 5144)\" dir=in action=allow protocol=TCP localport=5144", ignoreErrors: true);
        }

        private void StartService()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }
                }
            }
            catch
            {
                RunProcess("net.exe", $"start {ServiceName}", ignoreErrors: true);
            }
        }

        private void StopService()
        {
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                }
            }
            catch
            {
                RunProcess("net.exe", $"stop {ServiceName}", ignoreErrors: true);
            }
        }

        private void RunProcess(string fileName, string arguments, bool ignoreErrors = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) return;
                
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !ignoreErrors)
                {
                    throw new Exception($"Comando falló ({fileName}): {error}");
                }
            }
        }
    }
}
