using System;
using System.IO;
using System.Text.Json;

namespace GestionQ.Desktop;

public class AppConfig
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:5144";
    public double ZoomFactor { get; set; } = 1.0;

    private static readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestionQ");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}

