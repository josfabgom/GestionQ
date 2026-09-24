using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using GestionQ.Licensing;

namespace GestionQ.LicenseManager
{
    public partial class Form1 : Form
    {
        private const string PrivateKeyPem = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAv26WfFntPNhxslFis8VkoDFO/m1SyAWOFy9CQdX+8rIyRXH+
EVYGEa8s/zu0CKPWC0V4pqW6TLoSZ3v8DlIGnqBNOOTmYXfxO2ZZtxuQq9Oavz79
HHwUeJSRLC3tOlTmDJzKXLT8i2JV2+AHXUunW+MovA7aF+wBODKie62mETZpCTT+
nF1xKQzN8LZaH5DS6vfsPRbzb941rEJPAFO/oiplIJ9AWmjR54q1HIMBwf3OyvhY
Rv1x+1CgT1KDgwhlFa2emNMKTNfMrzNcIpWV9U0rnKl+XM41HImhRGp9LHqvcfY3
7eJiOMuBIBp1g5/0yU9bk+PRCQTbbc1RHnavIQIDAQABAoIBAQCbBCRcQj34PZWk
Pn9c8AWSKxu6sDCOxODRKXXP9khjo6VN0wdYZn63p1Eaxe/95x4XNoSC/kUkiLEK
l73+orG1lj2ySrm8R/JSlYkk2++FFO+E2q8AeAHjuvrr+azWxZUctYKxG+Y9wL2i
NYIBLJgzsnlz/9rf8D70kRw+ZtnJqQx8sZVuh4VU2z+AcM2u3VWDZ8XBlGXd8guC
uSpLEdl4g9mgllVVmwVlJ+WMCl37h4cvzIFGw31S25ZzzA3n95EOihPzKQgV4kld
U/A5Dcf5cHN2aBhJdw1b4n53MuFZKzpKDkaXUHBNvk22Chzy3u1TN6koYWM2TflL
dPnj0J65AoGBAOz46uh+9DKcKD6p1FZOAMg+tMNMiRpyPjbzX2391b0nD8xq5KHH
hcoB9joQXQlUK7NTp7fJnsA/mQErZUFneEyIqXuj+2+ofhpkMtWXHp101XOfOmXn
xotXCCHSTWxxq9iGJc51OelYdRAYj3IAqQcpQ+jbes7pHuVOjk1q3DaPAoGBAM7N
kOZAntF93l+yPYMyq7z8kCR3XpE17dM6GnkuRcLpLVJny6rMMSv/VTytAPZEtuqz
VZmTbistEBzyzuiAFYl8TQ0PWWNAIQK1N1rxPNQ/8TT/+TK14nBJUANvwMX/t98C
YwicbTLuGharBeaI9NL30uRSMNxsUjW2XYrwNRdPAoGBANXnNnvZJOqOJJGz0NR6
oqAeYiKr8lIp71jAxFJPv2B3Yv5dOrWBmZWnwa/V13U1QiEkEQ+H8kGM5rq0hjjM
gj1rWrkdYzf9+p4t9ejw+RSeQpKUly0nUwOx8sg8weBylvDi3juHe1fTng+Ca/E3
AVxSdlc5zpf4vAe2qiLdo5unAoGAQdPDZNd69lond7SnyeROMFkAlOr+SiCtdEgR
dzNYd2N7zrhFZzeaC1Q6UJcNMFbNFsZA4CLCtbGhaGWNoQpsUJglepvBK0uVdmQy
m5sgbrtvzxwPuamVy4I6mu1uolf0smLzHSGVzNlnqoGD3k8IB0NleNIExZUhUgGb
owc1DxcCgYBjS7UtE7m9rL+22urlANMw+NePwwSwGloAQ38hvgo/PHvU58Gh5gB7
YynCKHMKJXk+OclVwJZetkGNnE4GXT12YvygiQ94kjrtdJb0nYa6KAgXOMUcSGUR
SeCjfPuw/u4rTELoyWy4BFok+2hqHZmPIl1RI9mjssNIr7N+OsRW0A==
-----END RSA PRIVATE KEY-----";

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "GestionQ - Generador de Licencias";
            this.Size = new Size(500, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 10F);

            var lblHwid = new Label { Text = "Hardware ID del Cliente:", Location = new Point(20, 20), AutoSize = true };
            var txtHwid = new TextBox { Location = new Point(20, 45), Width = 440, Name = "txtHwid" };

            var lblClientName = new Label { Text = "Nombre del Cliente:", Location = new Point(20, 80), AutoSize = true };
            var txtClientName = new TextBox { Location = new Point(20, 105), Width = 440, Name = "txtClientName" };

            var lblExpiration = new Label { Text = "Expiración (Opcional):", Location = new Point(20, 140), AutoSize = true };
            var dtpExpiration = new DateTimePicker { Location = new Point(20, 165), Width = 200, Format = DateTimePickerFormat.Short, Name = "dtpExpiration", Checked = false, ShowCheckBox = true };

            var btnGenerate = new Button { Text = "Generar Licencia", Location = new Point(20, 210), Width = 210, Height = 40, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            
            var btnCopy = new Button { Text = "Copiar Licencia", Location = new Point(250, 210), Width = 210, Height = 40, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            var txtLicense = new TextBox { Location = new Point(20, 270), Width = 440, Height = 130, Multiline = true, ReadOnly = true, Name = "txtLicense" };

            btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtLicense.Text)) {
                    Clipboard.SetText(txtLicense.Text);
                    MessageBox.Show("¡Licencia copiada al portapapeles exitosamente!", "Copiado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnGenerate.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtHwid.Text)) {
                    MessageBox.Show("Ingrese un Hardware ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var license = new LicenseModel
                    {
                        HardwareId = txtHwid.Text.Trim(),
                        ClientName = txtClientName.Text.Trim(),
                        IssuedDate = DateTime.UtcNow,
                        ExpirationDate = dtpExpiration.Checked ? dtpExpiration.Value.ToUniversalTime() : null,
                        Signature = ""
                    };

                    using var rsa = RSA.Create();
                    rsa.ImportFromPem(PrivateKeyPem);

                    string dataToSign = license.ToJson();
                    byte[] dataBytes = Encoding.UTF8.GetBytes(dataToSign);
                    byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    license.Signature = Convert.ToBase64String(signatureBytes);

                    string finalJson = license.ToJson();
                    string base64License = Convert.ToBase64String(Encoding.UTF8.GetBytes(finalJson));

                    txtLicense.Text = base64License;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generando: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Controls.AddRange(new Control[] { lblHwid, txtHwid, lblClientName, txtClientName, lblExpiration, dtpExpiration, btnGenerate, btnCopy, txtLicense });
        }
    }
}