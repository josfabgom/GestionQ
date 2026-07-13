using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GestionQ.Desktop;

public partial class Form1 : Form
{
    private WebView2 webView;
    private string ticketToPrint = "";
    private AppConfig config;

    public Form1()
    {
        config = AppConfig.Load();
        InitializeComponent();
        this.Text = "GestionQ POS";
        this.WindowState = FormWindowState.Maximized;
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        
        webView = new WebView2();
        webView.Dock = DockStyle.Fill;
        
        var menuStrip = new MenuStrip();
        var opcionesMenu = new ToolStripMenuItem("Opciones");
        var configMenu = new ToolStripMenuItem("Configurar Conexión...");
        configMenu.Click += ConfigMenu_Click;
        opcionesMenu.DropDownItems.Add(configMenu);

        var zoomMenu = new ToolStripMenuItem("Escala (Zoom)");
        var zoomInMenu = new ToolStripMenuItem("Acercar");
        zoomInMenu.ShortcutKeyDisplayString = "Ctrl +";
        zoomInMenu.Click += (s, e) => { webView.ZoomFactor = Math.Min(webView.ZoomFactor + 0.1, 3.0); };
        
        var zoomOutMenu = new ToolStripMenuItem("Alejar");
        zoomOutMenu.ShortcutKeyDisplayString = "Ctrl -";
        zoomOutMenu.Click += (s, e) => { webView.ZoomFactor = Math.Max(webView.ZoomFactor - 0.1, 0.3); };
        
        var zoomResetMenu = new ToolStripMenuItem("Restablecer 100%");
        zoomResetMenu.ShortcutKeyDisplayString = "Ctrl 0";
        zoomResetMenu.Click += (s, e) => { webView.ZoomFactor = 1.0; };
        
        zoomMenu.DropDownItems.Add(zoomInMenu);
        zoomMenu.DropDownItems.Add(zoomOutMenu);
        zoomMenu.DropDownItems.Add(zoomResetMenu);
        opcionesMenu.DropDownItems.Add(zoomMenu);

        menuStrip.Items.Add(opcionesMenu);

        this.Controls.Add(webView);
        this.Controls.Add(menuStrip);
        this.MainMenuStrip = menuStrip;
        
        InitializeAsync();
    }

    async void InitializeAsync()
    {
        await webView.EnsureCoreWebView2Async(null);
        
        webView.ZoomFactor = config.ZoomFactor;
        webView.ZoomFactorChanged += (s, e) => SaveZoom();
        
        webView.CoreWebView2.NavigationCompleted += (s, e) =>
        {
            if (!e.IsSuccess && e.WebErrorStatus == Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.CannotConnect)
            {
                var result = MessageBox.Show($"No se pudo conectar al servidor en {config.ServerUrl}.\n\n¿Deseas configurar una nueva IP o URL?", "Error de Conexión", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                
                if (result == DialogResult.Yes)
                {
                    using var settings = new SettingsForm(config.ServerUrl);
                    if (settings.ShowDialog() == DialogResult.OK)
                    {
                        config.ServerUrl = settings.ServerUrl;
                        config.Save();
                        webView.CoreWebView2.Navigate(config.ServerUrl);
                    }
                }
                else
                {
                    // Reintentar en 3 segundos si el usuario decide no cambiarla
                    System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => 
                        this.Invoke(new Action(() => webView.CoreWebView2.Reload())));
                }
            }
        };

        // Cargar el sistema web local o remoto con un parámetro anti-caché
        string url = config.ServerUrl;
        url += (url.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
        webView.CoreWebView2.Navigate(url);

        // Escuchar mensajes desde el Javascript
        webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("command", out var cmdElement) && cmdElement.GetString() == "print")
            {
                if (root.TryGetProperty("text", out var textElement))
                {
                    string pName = "";
                    if (root.TryGetProperty("printerName", out var pElement))
                    {
                        pName = pElement.GetString() ?? "";
                    }
                    int copies = 1;
                    if (root.TryGetProperty("copies", out var copiesElement))
                    {
                        copies = copiesElement.GetInt32();
                    }
                    ticketToPrint = textElement.GetString() ?? "";
                    PrintTicket(ticketToPrint, pName, copies);
                }
            }
            else if (root.TryGetProperty("command", out var cmdElementImg) && cmdElementImg.GetString() == "printImage")
            {
                if (root.TryGetProperty("base64Image", out var b64Element))
                {
                    string pName = "";
                    if (root.TryGetProperty("printerName", out var pElement))
                    {
                        pName = pElement.GetString() ?? "";
                    }
                    int copies = 1;
                    if (root.TryGetProperty("copies", out var copiesElement))
                    {
                        copies = copiesElement.GetInt32();
                    }
                    
                    string base64 = b64Element.GetString() ?? "";
                    // Remove data:image/png;base64, prefix if present
                    if (base64.Contains(","))
                    {
                        base64 = base64.Substring(base64.IndexOf(",") + 1);
                    }
                    
                    PrintImageTicket(base64, pName, copies);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al recibir mensaje de impresión: " + ex.Message);
        }
    }

    private void PrintTicket(string text, string printerName = "", int copies = 1)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        try
        {
            PrintDocument pd = new PrintDocument();
            if (!string.IsNullOrEmpty(printerName))
            {
                pd.PrinterSettings.PrinterName = printerName;
            }
            
            pd.PrintPage += new PrintPageEventHandler(this.PrintPage);
            for (int i = 0; i < copies; i++)
            {
                pd.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al imprimir: " + ex.Message, "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintPage(object sender, PrintPageEventArgs e)
    {
        Font printFont = new Font("Courier New", 9, FontStyle.Bold);
        float yPos = 0;
        int count = 0;
        float leftMargin = 0;
        float topMargin = 0;

        if (e.Graphics == null) return;
        
        string[] lines = ticketToPrint.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach(var l in lines)
        {
            yPos = topMargin + (count * printFont.GetHeight(e.Graphics));
            e.Graphics.DrawString(l, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
            count++;
        }
    }

    private void PrintImageTicket(string base64Image, string printerName = "", int copies = 1)
    {
        if (string.IsNullOrEmpty(base64Image)) return;

        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using (var ms = new System.IO.MemoryStream(imageBytes))
            {
                Image printImg = Image.FromStream(ms);
                PrintDocument pd = new PrintDocument();
                if (!string.IsNullOrEmpty(printerName))
                {
                    pd.PrinterSettings.PrinterName = printerName;
                }

                pd.PrintPage += (sender, e) =>
                {
                    if (e.Graphics != null)
                    {
                        // Draw image taking the full width of the label paper
                        float targetWidth = e.PageBounds.Width;
                        float scale = targetWidth / printImg.Width;
                        float targetHeight = printImg.Height * scale;
                        e.Graphics.DrawImage(printImg, 0, 0, targetWidth, targetHeight);
                    }
                };

                for (int i = 0; i < copies; i++)
                {
                    pd.Print();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al imprimir imagen: " + ex.Message, "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveZoom()
    {
        config.ZoomFactor = webView.ZoomFactor;
        config.Save();
    }

    private void ConfigMenu_Click(object? sender, EventArgs e)
    {
        using var settings = new SettingsForm(config.ServerUrl);
        if (settings.ShowDialog() == DialogResult.OK)
        {
            config.ServerUrl = settings.ServerUrl;
            config.Save();
            
            // Re-navigate to the new URL with a cache buster
            string url = config.ServerUrl;
            url += (url.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
            webView.CoreWebView2.Navigate(url);
        }
    }
}
