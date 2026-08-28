using System;
using System.IO;
using System.Windows.Forms;

namespace GestionQ.CajaPOS
{
    public static class AppConfig
    {
        private static string ConfigFile = Path.Combine(Application.StartupPath, "server.txt");

        public static string ServerUrl
        {
            get
            {
                if (File.Exists(ConfigFile))
                {
                    var url = File.ReadAllText(ConfigFile).Trim();
                    if (url.EndsWith("/")) url = url.TrimEnd('/');
                    if (!url.StartsWith("http")) url = "http://" + url;
                    return url;
                }
                return "http://localhost:5144";
            }
            set
            {
                File.WriteAllText(ConfigFile, value);
            }
        }
    }
}
