using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestionQ.Desktop;

public class SplashForm : Form
{
    private readonly AppConfig _config;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts = new();

    private Label lblStatus = null!;
    private Label lblDetail = null!;
    private Label lblServerUrl = null!;
    private Panel statusDot = null!;
    private ProgressBar progressBar = null!;
    private Button btnStartService = null!;
    private Button btnConfig = null!;
    private Button btnExit = null!;

    private const string ServiceName = "GestionQ_Web_Service";
    private bool _isClosingSuccess;

    public SplashForm(AppConfig config)
    {
        _config = config;
        _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        InitializeUI();
        this.Shown += async (_, _) => await MonitorServiceLoopAsync();
        this.FormClosing += (_, _) => _cts.Cancel();
    }

    private void InitializeUI()
    {
        this.Text = "GestionQ - Iniciando";
        this.Size = new Size(490, 310);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(15, 23, 42); // Slate 900
        this.ForeColor = Color.White;
        this.ShowInTaskbar = true;
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        this.DoubleBuffered = true;

        // Borde sutil
        this.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 2);
            e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
        };

        // Barra superior de acento azul
        var topAccent = new Panel
        {
            Dock = DockStyle.Top,
            Height = 5,
            BackColor = Color.FromArgb(59, 130, 246) // Blue 500
        };
        this.Controls.Add(topAccent);

        // Ícono de la app
        var picIcon = new PictureBox
        {
            Size = new Size(48, 48),
            Location = new Point(32, 30),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        try
        {
            picIcon.Image = Icon.ExtractAssociatedIcon(Application.ExecutablePath)?.ToBitmap();
        }
        catch { }
        this.Controls.Add(picIcon);

        // Título y subtítulo
        var lblTitle = new Label
        {
            Text = "Bienvenido a GestionQ",
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(92, 26),
            AutoSize = true
        };
        this.Controls.Add(lblTitle);

        var lblSubtitle = new Label
        {
            Text = "Sistema de Gestión Comercial",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
            Location = new Point(94, 58),
            AutoSize = true
        };
        this.Controls.Add(lblSubtitle);

        // Tarjeta de estado del servicio
        var card = new Panel
        {
            Location = new Point(32, 98),
            Size = new Size(426, 118),
            BackColor = Color.FromArgb(30, 41, 59) // Slate 800
        };
        this.Controls.Add(card);

        statusDot = new Panel
        {
            Size = new Size(12, 12),
            Location = new Point(18, 22),
            BackColor = Color.FromArgb(59, 130, 246)
        };
        statusDot.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(statusDot.BackColor);
            e.Graphics.Clear(card.BackColor);
            e.Graphics.FillEllipse(brush, 0, 0, statusDot.Width - 1, statusDot.Height - 1);
        };
        card.Controls.Add(statusDot);

        lblStatus = new Label
        {
            Text = "Verificando el servicio del servidor...",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(38, 17),
            Size = new Size(374, 24)
        };
        card.Controls.Add(lblStatus);

        lblDetail = new Label
        {
            Text = "Comprobando disponibilidad de GestionQ...",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 46),
            Size = new Size(394, 34)
        };
        card.Controls.Add(lblDetail);

        progressBar = new ProgressBar
        {
            Location = new Point(18, 88),
            Size = new Size(390, 12),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25
        };
        card.Controls.Add(progressBar);

        // Botones de acción (visibles según necesidad)
        btnStartService = new Button
        {
            Text = "Iniciar Servicio Ahora",
            Location = new Point(32, 232),
            Size = new Size(165, 34),
            BackColor = Color.FromArgb(34, 197, 94), // Green 500
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnStartService.FlatAppearance.BorderSize = 0;
        btnStartService.Click += BtnStartService_Click;
        this.Controls.Add(btnStartService);

        btnConfig = new Button
        {
            Text = "Configurar IP...",
            Location = new Point(208, 232),
            Size = new Size(130, 34),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnConfig.FlatAppearance.BorderSize = 0;
        btnConfig.Click += BtnConfig_Click;
        this.Controls.Add(btnConfig);

        btnExit = new Button
        {
            Text = "Salir",
            Location = new Point(348, 232),
            Size = new Size(110, 34),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnExit.FlatAppearance.BorderSize = 0;
        btnExit.Click += (_, _) =>
        {
            _cts.Cancel();
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };
        this.Controls.Add(btnExit);

        lblServerUrl = new Label
        {
            Text = $"Servidor: {_config.ServerUrl}",
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(32, 280),
            AutoSize = true
        };
        this.Controls.Add(lblServerUrl);
    }

    private async Task MonitorServiceLoopAsync()
    {
        int attempt = 0;

        while (!_cts.IsCancellationRequested)
        {
            attempt++;

            bool isOnline = await CheckHttpOnlineAsync(_config.ServerUrl, _cts.Token);
            if (isOnline)
            {
                _isClosingSuccess = true;
                UpdateStatus(
                    "Servicio en línea",
                    "Conexión establecida correctamente. Abriendo GestionQ...",
                    Color.FromArgb(34, 197, 94),
                    showStartButton: false,
                    showConfigButton: false,
                    isCompleted: true);

                try { await Task.Delay(550, _cts.Token); } catch { }

                if (!this.IsDisposed)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                return;
            }

            if (IsLocalServer(_config.ServerUrl))
            {
                var svcState = await Task.Run(() => QueryWindowsServiceState(ServiceName));

                switch (svcState)
                {
                    case "START_PENDING":
                        UpdateStatus(
                            "El servicio se está levantando...",
                            "Windows está iniciando el servidor de GestionQ. Aguarde unos segundos...",
                            Color.FromArgb(245, 158, 11), // Amber 500
                            showStartButton: false,
                            showConfigButton: attempt >= 3);
                        break;

                    case "RUNNING":
                        UpdateStatus(
                            "Servicio activo, iniciando servidor web...",
                            "El proceso está en marcha. Esperando que el sistema esté listo...",
                            Color.FromArgb(59, 130, 246), // Blue 500
                            showStartButton: false,
                            showConfigButton: attempt >= 5);
                        break;

                    case "STOPPED":
                        UpdateStatus(
                            "El servicio está detenido o en espera",
                            "Puede estar aguardando el inicio retrasado de Windows, o puede iniciarlo ahora.",
                            Color.FromArgb(239, 68, 68), // Red 500
                            showStartButton: true,
                            showConfigButton: true);
                        break;

                    default:
                        UpdateStatus(
                            "Esperando al servidor local...",
                            $"No se recibió respuesta en {_config.ServerUrl}. Reintentando ({attempt})...",
                            Color.FromArgb(245, 158, 11),
                            showStartButton: false,
                            showConfigButton: true);
                        break;
                }
            }
            else
            {
                UpdateStatus(
                    "Conectando con el servidor...",
                    $"Esperando respuesta desde {_config.ServerUrl} (intento {attempt})...",
                    Color.FromArgb(245, 158, 11),
                    showStartButton: false,
                    showConfigButton: attempt >= 2);
            }

            try
            {
                await Task.Delay(1500, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<bool> CheckHttpOnlineAsync(string url, CancellationToken token)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            // Cualquier respuesta HTTP (200, 302 a Login, etc.) confirma que Kestrel ya está escuchando
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocalServer(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.IsLoopback ||
                   uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host == "127.0.0.1";
        }
        return false;
    }

    private static string QueryWindowsServiceState(string serviceName)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query {serviceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (proc == null) return "UNKNOWN";

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);

            if (output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)) return "RUNNING";
            if (output.Contains("START_PENDING", StringComparison.OrdinalIgnoreCase)) return "START_PENDING";
            if (output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase)) return "STOPPED";
            return "NOT_FOUND";
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private void UpdateStatus(string status, string detail, Color dotColor, bool showStartButton, bool showConfigButton, bool isCompleted = false)
    {
        if (this.IsDisposed) return;
        if (this.InvokeRequired)
        {
            this.BeginInvoke(() => UpdateStatus(status, detail, dotColor, showStartButton, showConfigButton, isCompleted));
            return;
        }

        lblStatus.Text = status;
        lblDetail.Text = detail;
        statusDot.BackColor = dotColor;
        statusDot.Invalidate();

        btnStartService.Visible = showStartButton;
        btnConfig.Visible = showConfigButton;

        if (isCompleted)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;
            btnExit.Enabled = false;
        }
    }

    private void BtnStartService_Click(object? sender, EventArgs e)
    {
        try
        {
            btnStartService.Enabled = false;
            UpdateStatus(
                "Iniciando el servicio...",
                "Solicitando a Windows que levante GestionQ Web Server...",
                Color.FromArgb(245, 158, 11),
                showStartButton: false,
                showConfigButton: false);

            Process.Start(new ProcessStartInfo
            {
                FileName = "net.exe",
                Arguments = $"start {ServiceName}",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar el servicio: " + ex.Message, "GestionQ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            if (!_isClosingSuccess && !this.IsDisposed)
            {
                btnStartService.Enabled = true;
            }
        }
    }

    private void BtnConfig_Click(object? sender, EventArgs e)
    {
        using var settings = new SettingsForm(_config.ServerUrl);
        if (settings.ShowDialog(this) == DialogResult.OK)
        {
            _config.ServerUrl = settings.ServerUrl;
            _config.Save();
            lblServerUrl.Text = $"Servidor: {_config.ServerUrl}";
            UpdateStatus(
                "Verificando nueva dirección...",
                $"Comprobando conexión con {_config.ServerUrl}...",
                Color.FromArgb(59, 130, 246),
                showStartButton: false,
                showConfigButton: true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
            _httpClient.Dispose();
        }
        base.Dispose(disposing);
    }
}
