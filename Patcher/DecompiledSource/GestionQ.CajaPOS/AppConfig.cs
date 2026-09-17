using System.IO;
using System.Windows.Forms;

namespace GestionQ.CajaPOS;

public static class AppConfig
{
	private static string ConfigFile = Path.Combine(Application.StartupPath, "server.txt");

	public static string ServerUrl
	{
		get
		{
			if (File.Exists(ConfigFile))
			{
				string text = File.ReadAllText(ConfigFile).Trim();
				if (text.EndsWith("/"))
				{
					text = text.TrimEnd('/');
				}
				if (!text.StartsWith("http"))
				{
					text = "http://" + text;
				}
				if (text.LastIndexOf(":") == text.IndexOf(":"))
				{
					text += ":5144";
				}
				return text;
			}
			return "http://localhost:5144";
		}
		set
		{
			File.WriteAllText(ConfigFile, value);
		}
	}
}
