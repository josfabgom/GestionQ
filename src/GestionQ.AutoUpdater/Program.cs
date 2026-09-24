using System;
using System.IO;
using System.Windows.Forms;

namespace GestionQ.AutoUpdater
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Defaults (can be overridden by args)
            string manifestUrl = "https://tudominio.com/updates/update_manifest.json";
            string mainExecutable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestionQ.Web.exe");
            string currentVersion = "1.0.0";

            if (args.Length >= 3)
            {
                manifestUrl = args[0];
                mainExecutable = args[1];
                currentVersion = args[2];
            }

            Application.Run(new Form1(manifestUrl, mainExecutable, currentVersion));
        }    
    }
}