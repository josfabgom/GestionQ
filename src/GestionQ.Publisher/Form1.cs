using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestionQ.Publisher
{
    public partial class Form1 : Form
    {
        private TextBox txtFtpHost, txtFtpUser, txtFtpPassword, txtFtpFolder, txtPublicDomain;
        private TextBox txtVersion, txtChangelog, txtZipFile;
        private Button btnBrowse, btnPublish;
        private Label lblStatus;
        private ProgressBar progressBar;

        public Form1()
        {
            InitializeComponentManual();
            LoadConfig();
        }

        private void InitializeComponentManual()
        {
            this.Text = "GestionQ - Publicador de Actualizaciones";
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(panel);

            int y = 20;
            
            // FTP Settings Group
            var grpFtp = new GroupBox { Text = "Configuración del Servidor FTP", Bounds = new Rectangle(20, y, 540, 150) };
            panel.Controls.Add(grpFtp);

            grpFtp.Controls.Add(new Label { Text = "Host FTP:", Bounds = new Rectangle(15, 25, 70, 20) });
            txtFtpHost = new TextBox { Bounds = new Rectangle(90, 25, 170, 20), PlaceholderText = "ftp://midominio.com" };
            grpFtp.Controls.Add(txtFtpHost);

            grpFtp.Controls.Add(new Label { Text = "Usuario:", Bounds = new Rectangle(280, 25, 60, 20) });
            txtFtpUser = new TextBox { Bounds = new Rectangle(350, 25, 170, 20) };
            grpFtp.Controls.Add(txtFtpUser);

            grpFtp.Controls.Add(new Label { Text = "Contraseña:", Bounds = new Rectangle(15, 55, 70, 20) });
            txtFtpPassword = new TextBox { Bounds = new Rectangle(90, 55, 170, 20), PasswordChar = '*' };
            grpFtp.Controls.Add(txtFtpPassword);

            grpFtp.Controls.Add(new Label { Text = "Carpeta remota:", Bounds = new Rectangle(280, 55, 90, 20) });
            txtFtpFolder = new TextBox { Bounds = new Rectangle(370, 55, 150, 20), PlaceholderText = "/updates/" };
            grpFtp.Controls.Add(txtFtpFolder);

            grpFtp.Controls.Add(new Label { Text = "Dominio Público (Ej: https://midominio.com/updates/):", Bounds = new Rectangle(15, 85, 300, 20) });
            txtPublicDomain = new TextBox { Bounds = new Rectangle(15, 105, 505, 20) };
            grpFtp.Controls.Add(txtPublicDomain);

            y += 170;

            // Update Details Group
            var grpUpdate = new GroupBox { Text = "Datos de la Actualización", Bounds = new Rectangle(20, y, 540, 280) };
            panel.Controls.Add(grpUpdate);

            grpUpdate.Controls.Add(new Label { Text = "Versión a publicar (Ej: 1.0.6):", Bounds = new Rectangle(15, 25, 200, 20) });
            txtVersion = new TextBox { Bounds = new Rectangle(15, 45, 150, 20) };
            grpUpdate.Controls.Add(txtVersion);

            grpUpdate.Controls.Add(new Label { Text = "Archivo ZIP con los ejecutables:", Bounds = new Rectangle(15, 75, 200, 20) });
            txtZipFile = new TextBox { Bounds = new Rectangle(15, 95, 415, 20), ReadOnly = true };
            grpUpdate.Controls.Add(txtZipFile);

            btnBrowse = new Button { Text = "Examinar...", Bounds = new Rectangle(440, 93, 80, 25) };
            btnBrowse.Click += (s, e) => {
                using var dlg = new OpenFileDialog { Filter = "Archivos ZIP|*.zip" };
                if (dlg.ShowDialog() == DialogResult.OK) txtZipFile.Text = dlg.FileName;
            };
            grpUpdate.Controls.Add(btnBrowse);

            grpUpdate.Controls.Add(new Label { Text = "Novedades / Changelog (Una por línea):", Bounds = new Rectangle(15, 125, 300, 20) });
            txtChangelog = new TextBox { Bounds = new Rectangle(15, 145, 505, 115), Multiline = true, ScrollBars = ScrollBars.Vertical };
            grpUpdate.Controls.Add(txtChangelog);

            y += 290;

            btnPublish = new Button { Text = "Subir y Publicar Actualización", Bounds = new Rectangle(20, y, 540, 40), BackColor = Color.LightGreen, Font = new Font(this.Font, FontStyle.Bold) };
            btnPublish.Click += BtnPublish_Click;
            panel.Controls.Add(btnPublish);

            y += 50;
            progressBar = new ProgressBar { Bounds = new Rectangle(20, y, 540, 20) };
            panel.Controls.Add(progressBar);

            y += 25;
            lblStatus = new Label { Bounds = new Rectangle(20, y, 540, 40), Text = "Listo para publicar." };
            panel.Controls.Add(lblStatus);
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists("publisher_config.json"))
                {
                    var conf = JsonSerializer.Deserialize<Config>(File.ReadAllText("publisher_config.json"));
                    if (conf != null)
                    {
                        txtFtpHost.Text = conf.FtpHost;
                        txtFtpUser.Text = conf.FtpUser;
                        txtFtpPassword.Text = conf.FtpPassword;
                        txtFtpFolder.Text = conf.FtpFolder;
                        txtPublicDomain.Text = conf.PublicDomain;
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                var conf = new Config
                {
                    FtpHost = txtFtpHost.Text,
                    FtpUser = txtFtpUser.Text,
                    FtpPassword = txtFtpPassword.Text,
                    FtpFolder = txtFtpFolder.Text,
                    PublicDomain = txtPublicDomain.Text
                };
                File.WriteAllText("publisher_config.json", JsonSerializer.Serialize(conf));
            }
            catch { }
        }

        private async void BtnPublish_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVersion.Text) || string.IsNullOrWhiteSpace(txtZipFile.Text))
            {
                MessageBox.Show("Debe indicar la versión y seleccionar el archivo ZIP.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveConfig();

            btnPublish.Enabled = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Conectando al servidor FTP y subiendo ZIP...";

            try
            {
                string ftpHost = txtFtpHost.Text.Trim();
                if (!ftpHost.StartsWith("ftp://")) ftpHost = "ftp://" + ftpHost;
                if (!ftpHost.EndsWith("/")) ftpHost += "/";

                string safeRemoteFolder = txtFtpFolder.Text.Trim().TrimStart('/').TrimEnd('/');
                if (!string.IsNullOrEmpty(safeRemoteFolder)) safeRemoteFolder += "/";

                string publicDomain = txtPublicDomain.Text.Trim();
                if (!publicDomain.EndsWith("/")) publicDomain += "/";

                string zipFileName = $"GestionQ_v{txtVersion.Text}.zip";
                string zipRequestUri = $"{ftpHost}{safeRemoteFolder}{zipFileName}";

                // Subir ZIP
                var request = (FtpWebRequest)WebRequest.Create(zipRequestUri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                if (!string.IsNullOrEmpty(txtFtpUser.Text))
                    request.Credentials = new NetworkCredential(txtFtpUser.Text, txtFtpPassword.Text);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                byte[] fileBytes = File.ReadAllBytes(txtZipFile.Text);
                request.ContentLength = fileBytes.Length;

                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                using (var response = (FtpWebResponse)await request.GetResponseAsync()) { }

                lblStatus.Text = "ZIP subido. Generando y subiendo update_manifest.json...";

                // Subir Manifest
                var manifest = new
                {
                    Version = txtVersion.Text,
                    ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    DownloadUrl = $"{publicDomain}{zipFileName}",
                    Changelog = txtChangelog.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                };

                string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                byte[] manifestBytes = System.Text.Encoding.UTF8.GetBytes(manifestJson);

                string manifestRequestUri = $"{ftpHost}{safeRemoteFolder}update_manifest.json";
                var manifestReq = (FtpWebRequest)WebRequest.Create(manifestRequestUri);
                manifestReq.Method = WebRequestMethods.Ftp.UploadFile;
                if (!string.IsNullOrEmpty(txtFtpUser.Text))
                    manifestReq.Credentials = new NetworkCredential(txtFtpUser.Text, txtFtpPassword.Text);
                manifestReq.UsePassive = true;
                manifestReq.UseBinary = true;
                manifestReq.KeepAlive = false;
                manifestReq.ContentLength = manifestBytes.Length;

                using (var requestStream = await manifestReq.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(manifestBytes, 0, manifestBytes.Length);
                }
                using (var response = (FtpWebResponse)await manifestReq.GetResponseAsync()) { }

                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                lblStatus.Text = $"¡Éxito! Versión {txtVersion.Text} publicada correctamente.";
                MessageBox.Show("Actualización publicada en la nube con éxito.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                lblStatus.Text = "Error: " + ex.Message;
                MessageBox.Show("Ocurrió un error al subir los archivos:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnPublish.Enabled = true;
            }
        }

        private class Config
        {
            public string FtpHost { get; set; }
            public string FtpUser { get; set; }
            public string FtpPassword { get; set; }
            public string FtpFolder { get; set; }
            public string PublicDomain { get; set; }
        }
    }
}
