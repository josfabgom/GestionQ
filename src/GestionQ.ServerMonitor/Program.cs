using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace GestionQ.ServerMonitor;

static class Program
{
    [STAThread]
    static void Main()
    {
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "start.txt"), "Starting app...");
        try
        {
            ApplicationConfiguration.Initialize();
            var context = new MonitorApplicationContext();
            Application.Run();
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), ex.ToString());
            MessageBox.Show("Error fatal: " + ex.Message);
        }
    }
}

public class MonitorApplicationContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip contextMenu;
    private Timer pollTimer;
    private HttpClient httpClient;
    private bool isServerOnline = false;

    public MonitorApplicationContext()
    {
        httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(2);

        contextMenu = new ContextMenuStrip();
        
        var openAppMenuItem = new ToolStripMenuItem("Abrir GestionQ", null, OpenApp);
        openAppMenuItem.Font = new Font(openAppMenuItem.Font, FontStyle.Bold);
        
        contextMenu.Items.Add(openAppMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Iniciar Servidor", null, StartServer);
        contextMenu.Items.Add("Detener Servidor", null, StopServer);
        contextMenu.Items.Add("Reiniciar Servidor", null, RestartServer);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Salir", null, Exit);

        trayIcon = new NotifyIcon()
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "GestionQ: Comprobando..."
        };

        trayIcon.DoubleClick += OpenApp;

        pollTimer = new Timer();
        pollTimer.Interval = 5000; // 5 segundos
        pollTimer.Tick += PollTimer_Tick;
        pollTimer.Start();
        
        // Ejecutar primer chequeo de inmediato
        PollTimer_Tick(null, null);
    }

    private async void PollTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var response = await httpClient.GetAsync("http://127.0.0.1:5144/");
            if (response.IsSuccessStatusCode)
            {
                if (!isServerOnline)
                {
                    isServerOnline = true;
                    SetOnline();
                }
            }
            else
            {
                SetOffline();
            }
        }
        catch
        {
            SetOffline();
        }
    }

    private void SetOnline()
    {
        if (trayIcon != null)
        {
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            trayIcon.Text = "GestionQ: En línea";
        }
    }

    private void SetOffline()
    {
        if (isServerOnline || trayIcon.Text.Contains("Comprobando"))
        {
            isServerOnline = false;
            trayIcon.Icon = SystemIcons.Error; // Error para offline
            trayIcon.Text = "GestionQ: Desconectado";
        }
    }

    private string GetRootPath()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Iniciar-GestionQ.vbs")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return dir ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    private void OpenApp(object? sender, EventArgs e)
    {
        try
        {
            string rootPath = GetRootPath();
            string scriptPath = Path.Combine(rootPath, "Iniciar-GestionQ.vbs");
            if (File.Exists(scriptPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wscript.exe",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("No se encontró Iniciar-GestionQ.vbs en " + rootPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar la aplicación: " + ex.Message);
        }
    }

    private void StartServer(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "net.exe",
                Arguments = "start GestionQ_Web_Service",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
            trayIcon.ShowBalloonTip(3000, "GestionQ", "Iniciando servicio de Windows...", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar el servicio: " + ex.Message);
        }
    }

    private void StopServer(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "net.exe",
                Arguments = "stop GestionQ_Web_Service",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
            trayIcon.ShowBalloonTip(3000, "GestionQ", "Deteniendo servicio de Windows...", ToolTipIcon.Warning);
            SetOffline();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo detener el servicio: " + ex.Message);
        }
    }

    private void RestartServer(object? sender, EventArgs e)
    {
        StopServer(sender, e);
        // Esperar un momento antes de reiniciar
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
        {
            this.contextMenu.Invoke(new Action(() => StartServer(sender, e)));
        });
    }

    private void Exit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}