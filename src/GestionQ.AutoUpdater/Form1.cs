using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestionQ.AutoUpdater
{
    public partial class Form1 : Form
    {
        private string _manifestUrl;
        private string _mainExecutablePath;
        private string _currentVersion;
        private UpdateManifest _manifest;

        private Label lblStatus;
        private ProgressBar progressBar1;
        private TextBox txtChangelog;
        private Button btnUpdate;

        public Form1(string manifestUrl, string mainExecutablePath, string currentVersion)
        {
            _manifestUrl = manifestUrl;
            _mainExecutablePath = mainExecutablePath;
            _currentVersion = currentVersion;
            
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Text = "GestionQ - Actualizador Automático";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(panel);

            lblStatus = new Label 
            { 
                Text = "Buscando actualizaciones...", 
                Dock = DockStyle.Top, 
                Height = 40,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };
            
            progressBar1 = new ProgressBar 
            { 
                Dock = DockStyle.Top, 
                Height = 20,
                Style = ProgressBarStyle.Marquee 
            };

            var lblNovedades = new Label 
            { 
                Text = "Novedades:", 
                Dock = DockStyle.Top, 
                Height = 30,
                Padding = new Padding(0, 10, 0, 0)
            };

            txtChangelog = new TextBox 
            { 
                Multiline = true, 
                Dock = DockStyle.Top, 
                Height = 150, 
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = System.Drawing.SystemColors.Window
            };

            btnUpdate = new Button 
            { 
                Text = "Actualizar Ahora", 
                Dock = DockStyle.Bottom, 
                Height = 40,
                Enabled = false,
                BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            panel.Controls.Add(txtChangelog);
            panel.Controls.Add(lblNovedades);
            panel.Controls.Add(progressBar1);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(btnUpdate);
            
            this.Load += Form1_Load;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try 
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(_manifestUrl);
                _manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (IsNewerVersion(_currentVersion, _manifest.Version))
                {
                    lblStatus.Text = $"Nueva versión disponible: {_manifest.Version}\nLanzada el: {_manifest.ReleaseDate}";
                    txtChangelog.Text = string.Join(Environment.NewLine, _manifest.Changelog);
                    btnUpdate.Enabled = true;
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    progressBar1.Value = 0;
                }
                else
                {
                    lblStatus.Text = "El sistema ya está en la última versión.";
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    progressBar1.Value = 100;
                    btnUpdate.Text = "Cerrar";
                    btnUpdate.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error al buscar actualizaciones: " + ex.Message;
                progressBar1.Style = ProgressBarStyle.Continuous;
                btnUpdate.Text = "Cerrar";
                btnUpdate.Enabled = true;
            }
        }

        private bool IsNewerVersion(string current, string remote)
        {
            if (Version.TryParse(current, out var c) && Version.TryParse(remote, out var r))
            {
                return r > c;
            }
            return false;
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (btnUpdate.Text == "Cerrar")
            {
                Application.Exit();
                return;
            }

            btnUpdate.Enabled = false;
            lblStatus.Text = "Descargando actualización...";
            progressBar1.Style = ProgressBarStyle.Continuous;
            
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"update_{_manifest.Version}.zip");
                
                using (var client = new HttpClient())
                {
                    using var response = await client.GetAsync(_manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes != -1)
                        {
                            int progress = (int)((totalRead * 100) / totalBytes);
                            progressBar1.Value = progress;
                        }
                    }
                }

                lblStatus.Text = "Cerrando procesos...";
                // Cerrar procesos actuales para no bloquear archivos
                var processes = Process.GetProcessesByName("GestionQ.Web")
                    .Concat(Process.GetProcessesByName("GestionQ.Desktop"))
                    .Concat(Process.GetProcessesByName("GestionQ.CajaPOS"))
                    .ToList();
                    
                foreach (var p in processes)
                {
                    try { p.Kill(); p.WaitForExit(); } catch { }
                }
                
                await Task.Delay(1000); 

                lblStatus.Text = "Instalando actualización...";
                progressBar1.Style = ProgressBarStyle.Marquee;
                
                string appDir = Path.GetDirectoryName(_mainExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                
                await Task.Run(() => 
                {
                    using var archive = ZipFile.OpenRead(tempFile);
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(appDir, entry.FullName));
                        if (destinationPath.StartsWith(appDir, StringComparison.Ordinal))
                        {
                            string dir = Path.GetDirectoryName(destinationPath);
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            
                            if (string.IsNullOrEmpty(entry.Name)) continue; // Es una carpeta
                            
                            // Evitar sobreescribir el actualizador si está en ejecución
                            if (entry.Name.Equals("GestionQ.AutoUpdater.exe", StringComparison.OrdinalIgnoreCase)) continue;
                            if (entry.Name.Equals("GestionQ.AutoUpdater.dll", StringComparison.OrdinalIgnoreCase)) continue;

                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                });

                // Guardar historial de actualizacion
                try 
                {
                    string historyFile = Path.Combine(appDir, "update_history.json");
                    var history = new List<UpdateManifest>();
                    if (File.Exists(historyFile))
                    {
                        history = JsonSerializer.Deserialize<List<UpdateManifest>>(File.ReadAllText(historyFile));
                    }
                    history.Add(_manifest);
                    File.WriteAllText(historyFile, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
                } catch { }

                lblStatus.Text = "Actualización completada. Reiniciando...";
                await Task.Delay(1500);

                if (File.Exists(_mainExecutablePath))
                {
                    Process.Start(new ProcessStartInfo { FileName = _mainExecutablePath, UseShellExecute = true, WorkingDirectory = appDir });
                }
                
                Application.Exit();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error al actualizar: " + ex.Message;
                progressBar1.Style = ProgressBarStyle.Continuous;
                btnUpdate.Text = "Cerrar";
                btnUpdate.Enabled = true;
            }
        }
    }

    public class UpdateManifest
    {
        public string Version { get; set; }
        public string ReleaseDate { get; set; }
        public string DownloadUrl { get; set; }
        public string[] Changelog { get; set; }
    }
}
