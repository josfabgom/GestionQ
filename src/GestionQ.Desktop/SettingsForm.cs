using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestionQ.Desktop;

public class SettingsForm : Form
{
    private TextBox txtUrl;
    private Button btnSave;

    public string ServerUrl => txtUrl.Text.Trim();

    public SettingsForm(string currentUrl)
    {
        this.Text = "Configuración del Servidor";
        this.Size = new Size(400, 150);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        Label lbl = new Label() { Text = "URL del Servidor (ej. http://192.168.1.100:5144):", Left = 15, Top = 15, Width = 350 };
        txtUrl = new TextBox() { Text = currentUrl, Left = 15, Top = 40, Width = 350 };
        
        btnSave = new Button() { Text = "Guardar", Left = 265, Top = 75, Width = 100 };
        btnSave.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

        this.Controls.Add(lbl);
        this.Controls.Add(txtUrl);
        this.Controls.Add(btnSave);
        this.AcceptButton = btnSave;
    }
}
