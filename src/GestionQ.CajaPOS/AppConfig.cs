using System;
using System.IO;
using System.Windows.Forms;

namespace GestionQ.CajaPOS
{
    public static class AppConfig
    {
        private static string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestionQ");
        private static string ConfigFile = Path.Combine(ConfigDir, "server.txt");

        public static string ServerUrl
        {
            get
            {
                if (File.Exists(ConfigFile))
                {
                    var url = File.ReadAllText(ConfigFile).Trim();
                    if (url.EndsWith("/")) url = url.TrimEnd('/');
                    if (!url.StartsWith("http")) url = "http://" + url; if (url.LastIndexOf(":") == url.IndexOf(":")) url = url + ":5144";
                    return url;
                }
                return "http://localhost:5144";
            }
            set
            {
                if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
                File.WriteAllText(ConfigFile, value);
            }
        }
    }
}


